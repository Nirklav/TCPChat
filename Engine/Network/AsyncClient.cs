using Engine.API.StandardAPI;
using Engine.API.StandardAPI.ServerCommands;
using Engine.Connections;
using Engine.Containers;
using Engine.Model.Client;
using Engine.Network.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Engine.Network
{
  /// <summary>
  /// Клиентское соединение.
  /// </summary>
  public sealed class AsyncClient : Connection
  {
    #region consts
    public const int CryptorKeySize = 2048;
    public const long DefaultFilePartSize = 500 * 1024;
    private const int MaxReceivedDataSize = 1024 * 1024;
    private const int SystemTimerInterval = 1000;
    private const int ReconnectTimeInterval = 10 * 1000;
    private const int PingInterval = 3000;

    private static readonly SocketError[] reconnectErrorsList = new SocketError[] 
    { 
      SocketError.NetworkReset, SocketError.ConnectionAborted,
      SocketError.ConnectionReset, SocketError.TimedOut,
      SocketError.HostDown
    };

    #endregion

    #region private fields
    private IPEndPoint hostAddress;

    private RSACryptoServiceProvider keyCryptor;
    private SynchronizationContext GUIContext;

    private bool waitingAPIName;
    private string serverAPIVersion;
    private long lastTempId;

    private bool reconnect;
    private bool reconnecting;
    private DateTime lastReconnect;
    private DateTime lastPingRequest;

    private Dictionary<string, PeerConnection> peers;
    private List<WaitingCommandContainer> waitingCommands;

    private Timer systemTimer;
    #endregion

    #region constructors
    /// <summary>
    /// Создает клиентское подключение к серверу.
    /// </summary>
    public AsyncClient(string nick)
    {
      keyCryptor = new RSACryptoServiceProvider(CryptorKeySize);
      GUIContext = SynchronizationContext.Current;

      peers = new Dictionary<string, PeerConnection>();
      waitingCommands = new List<WaitingCommandContainer>();
      waitingAPIName = false;
      reconnecting = false;
      reconnect = true;
      lastTempId = 0;
      Id = nick;
    }
    #endregion

    #region properties/events
    /// <summary>
    /// Взвращает значение, характеризующее подключен ли клиент к серверу.
    /// </summary>
    public bool IsConnected
    {
      get
      {
        ThrowIfDisposed();
        return handler.Connected;
      }
    }

    /// <summary>
    /// Открытый ключ данного соединения.
    /// </summary>
    public RSAParameters OpenKey
    {
      get
      {
        ThrowIfDisposed();
        return keyCryptor.ExportParameters(false);
      }
    }

    /// <summary>
    /// Асинхронный алгоритм шифрования, данного соединения.
    /// </summary>
    public RSACryptoServiceProvider KeyCryptor
    {
      get
      {
        ThrowIfDisposed();
        return keyCryptor;
      }
    }

    /// <summary>
    /// Версия API используемая на сервере.
    /// </summary>
    public string ServerAPIVersion
    {
      get
      {
        ThrowIfDisposed();

        if (serverAPIVersion == null)
          return string.Empty;

        return serverAPIVersion;
      }
    }

    /// <summary>
    /// Задает или возращает значение которое характеризует будет 
    /// ли клиент после потери связи пытатся пересоеденится с сервером.
    /// </summary>
    public bool Reconnect
    {
      get
      {
        ThrowIfDisposed();
        return reconnect;
      }
      set
      {
        ThrowIfDisposed();
        reconnect = value;
      }
    }

    #endregion

    #region public methods
    /// <summary>
    /// Асинхронно соединяет клиент с сервером.
    /// </summary>
    /// <param name="ServerAddress">Адресс сервера.</param>
    public void Connect(IPEndPoint serverAddress)
    {
      ThrowIfDisposed();

      if (handler != null)
        if (handler.Connected == true)
          throw new SocketException((int)SocketError.IsConnected);

      waitingAPIName = true;
      hostAddress = serverAddress;
      systemTimer = new Timer(SystemTimerCallback, null, SystemTimerInterval, -1);
      Socket connectingHandler = new Socket(serverAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      connectingHandler.BeginConnect(serverAddress, ConnectCallback, connectingHandler);
    }

    /// <summary>
    /// Перерегесрирует соединение на новый id, и пытается соеденится к удаленной точке.
    /// </summary>
    /// <param name="connectId">Иденификатор выданный после подключения к P2P сервису.</param>
    /// <param name="newId">Новый индетификатор.</param>
    /// <param name="userEndPoint">Точка подключения.</param>
    public void RegisterAndConnect(string connectId, string newId, IPEndPoint userEndPoint)
    {
      lock (peers)
      {
        PeerConnection connection;

        if (!peers.TryGetValue(connectId, out connection))
          throw new ArgumentException("Нет соединения с таким connectID");

        connection.Disconnect();

        connection.Id = newId;
        peers.Remove(connectId);
        peers.Add(newId, connection);

        connection.ConnectToPeer(userEndPoint);
      }
    }

    /// <summary>
    /// Перерегесрирует соединение на новый id, и ожидает подключение.
    /// </summary>
    /// <param name="connectId">Иденификатор выданный после подключения к P2P сервису.</param>
    /// <param name="newId">Новый индетификатор.</param>
    /// <param name="userEndPoint">Точка откуда следует ожидать подключение.</param>
    internal void RegisterAndWait(string connectId, string newId, IPEndPoint userEndPoint)
    {
      lock (peers)
      {
        PeerConnection connection;

        if (!peers.TryGetValue(connectId, out connection))
          throw new ArgumentException("Нет соединения с таким connectID");

        connection.Disconnect();

        connection.Id = newId;
        peers.Remove(connectId);
        peers.Add(newId, connection);

        connection.WaitConnection(userEndPoint);
      }
    }

    /// <summary>
    /// Перерегесрирует соединение.
    /// </summary>
    /// <param name="oldId">Старый индетефикартор.</param>
    /// <param name="newId">Новый индетификатор.</param>
    internal void Register(string oldId, string newId)
    {
      lock (peers)
      {
        PeerConnection connection;

        if (!peers.TryGetValue(oldId, out connection))
          throw new ArgumentException("Нет соединения с таким connectID");

        connection.Id = newId;
        peers.Remove(oldId);
        peers.Add(newId, connection);
      }
    }

    /// <summary>
    /// Асинхронно отправляет команду напрямую к пользователю. 
    /// Если подключение с пользователем еще не создано, оно будет создано.
    /// </summary>
    /// <param name="info">Индетификатор соединение которому необходимо отправить команду.</param>
    /// <param name="id">Индетификатор команды.</param>
    /// <param name="messageContent">Содержимое команды.</param>
    public void SendMessage(string peerId, ushort id, object messageContent)
    {
      ThrowIfDisposed();

      lock(peers)
      {
        PeerConnection connection;
        peers.TryGetValue(peerId, out connection);

        if (connection != null)
          connection.SendMessage(id, messageContent);
        else
        {
          lock (waitingCommands)
            waitingCommands.Add(new WaitingCommandContainer(peerId, id, messageContent));

          if (ClientModel.API != null)
            ClientModel.API.ConnectToPeer(peerId);
        }
      }
    }

    /// <summary>
    /// Создает несоединенное подключение, зарегистрированное в данном клиенте.
    /// </summary>
    /// <returns>Прямое подключение к пользовтелю. (Не соединенное)</returns>
    public PeerConnection CreatePeerConnection()
    {
      ThrowIfDisposed();

      PeerConnection peer = new PeerConnection();

      lock (peers)
      {
        peer.Id = string.Format("{0}{1}", TempConnectionPrefix, lastTempId++);
        peers.Add(peer.Id, peer);
      }

      peer.AsyncError += PeerAsyncError;
      peer.DataReceived += PeerDataReceived;
      peer.Connected += PeerConnected;
      peer.Disconnected += PeerDisconnect;

      return peer;
    }
    #endregion

    #region private/protected override methods
    private void ConnectCallback(IAsyncResult result)
    {
      try
      {
        Socket Handler = (Socket)result.AsyncState;
        Handler.EndConnect(result);

        Construct(Handler, MaxReceivedDataSize);
        reconnecting = false;
      }
      catch (SocketException se)
      {
        if (se.SocketErrorCode == SocketError.ConnectionRefused)
          reconnecting = true;
        else
          ClientModel.OnConnected(this, new ConnectEventArgs { Error = se });
      }
      catch (Exception e)
      {
        ClientModel.OnConnected(this, new ConnectEventArgs { Error = e });
      }
    }

    private void PeerConnected(object sender, ConnectEventArgs e)
    {
      if (e.Error != null)
      {
        ClientModel.OnAsyncError(this, new AsyncErrorEventArgs { Error = e.Error });
        return;
      }

      PeerConnection peerConnection = (PeerConnection)sender;

      lock (waitingCommands)
      {
        IEnumerable<WaitingCommandContainer> commands = waitingCommands
          .Where(command => string.Equals(command.ConnectionId, peerConnection.Id))
          .ToArray();

        foreach (WaitingCommandContainer command in commands)
        {
          peerConnection.SendMessage(command.CommandId, command.MessageContent);
          waitingCommands.Remove(command);
        }
      }
    }

    private void PeerDisconnect(object sender, ConnectEventArgs e)
    {
      if (e.Error != null)
      {
        ClientModel.OnAsyncError(this, new AsyncErrorEventArgs { Error = e.Error });
        return;
      }

      PeerConnection peerConnection = (PeerConnection)sender;

      lock (peers)
      {
        if (peers.ContainsKey(peerConnection.Id))
          peers.Remove(peerConnection.Id);

        peerConnection.Dispose();
      }
    }

    private void PeerAsyncError(object sender, AsyncErrorEventArgs e)
    {
      if (e.Error != null)
      {
        ClientModel.OnAsyncError(this, new AsyncErrorEventArgs { Error = e.Error });
        return;
      }
    }

    private void PeerDataReceived(object sender, DataReceivedEventArgs e)
    {
      try
      {
        if (e.Error != null)
          throw e.Error;

        IClientAPICommand command = ClientModel.API.GetCommand(e.ReceivedData);
        ClientCommandArgs args = new ClientCommandArgs
        {
          Message = e.ReceivedData,
          PeerConnectionId = ((IConnection)sender).Id
        };

        command.Run(args);
      }
      catch (Exception exc)
      {
        ClientModel.OnAsyncError(this, new AsyncErrorEventArgs { Error = exc });
      }
    }

    protected override void OnDataReceived(DataReceivedEventArgs e)
    {
      try
      {
        if (e.Error != null)
          throw e.Error;

        if (waitingAPIName)
        {
          serverAPIVersion = Encoding.Unicode.GetString(e.ReceivedData);

          switch (serverAPIVersion)
          {
            case StandardServerAPI.API:
              ClientModel.API = new StandardClientAPI();
              break;
          }
          
          if (ClientModel.API != null)
          {
            ClientModel.OnConnected(this, new ConnectEventArgs { Error = null });
            waitingAPIName = false;
          }
          else
            throw new APINotSupprtedException(serverAPIVersion);

          return;
        }

        IClientAPICommand command = ClientModel.API.GetCommand(e.ReceivedData);
        command.Run(new ClientCommandArgs { Message = e.ReceivedData });
      }
      catch (Exception exc)
      {
        
        ClientModel.OnAsyncError(this, new AsyncErrorEventArgs { Error = exc });
      }
    }

    public override void Dispose()
    {
      keyCryptor.Clear();

      if (systemTimer != null)
        systemTimer.Dispose();

      lock (peers)
      {
        foreach (string currentId in peers.Keys)
          peers[currentId].Dispose();

        peers.Clear();
      }

      base.Dispose();
    }

    protected override void OnDataSended(DataSendedEventArgs args)
    {
      if (args.Error != null)
        ClientModel.OnAsyncError(this, new AsyncErrorEventArgs { Error = args.Error });
    }

    protected override bool HandleSocketException(SocketException se)
    {
      if (!reconnect)
        return false;

      if (!reconnectErrorsList.Contains(se.SocketErrorCode))
        return false;

      reconnecting = true;
      lastReconnect = DateTime.Now;
      return true;
    }

    private void SystemTimerCallback(object state)
    {
      try
      {
        if (handler != null && IsConnected)
        {
          if ((DateTime.Now - lastPingRequest).TotalMilliseconds >= PingInterval)
          {
            SendMessage(ServerPingRequestCommand.Id, null);

            lastPingRequest = DateTime.Now;
          }

          lock (peers)
          {
            List<string> keysList = peers.Keys.ToList();
            foreach(string currentId in keysList)
              if (peers[currentId].IntervalOfSilence >= PeerConnection.ConnectionTimeOut)
              {
                peers.Remove(currentId);
                peers[currentId].Dispose();
              }
          }
        }

        if (reconnecting)
        {
          if ((DateTime.Now - lastReconnect).TotalMilliseconds >= ReconnectTimeInterval)
          {
            ClientModel.OnSystemMessage("Попытка соединения с сервером...");

            if (handler != null)
              handler.Close();

            Connect(hostAddress);

            lastReconnect = DateTime.Now;
          }
        }

        systemTimer.Change(SystemTimerInterval, -1);
      }
      catch (ObjectDisposedException) { }
    }
    #endregion
  }
}
