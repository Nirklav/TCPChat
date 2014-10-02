using Engine.API.StandardAPI;
using Engine.API.StandardAPI.ServerCommands;
using Engine.Containers;
using Engine.Exceptions;
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

    private bool waitingAPIName;
    private string serverAPIVersion;

    private bool reconnect;
    private bool reconnecting;
    private DateTime lastReconnect;
    private DateTime lastPingRequest;

    private object timerSync = new object();
    private Timer systemTimer;
    #endregion

    #region constructors
    /// <summary>
    /// Создает клиентское подключение к серверу.
    /// </summary>
    public AsyncClient(string nick)
    {
      keyCryptor = new RSACryptoServiceProvider(CryptorKeySize);

      waitingAPIName = false;
      reconnecting = false;
      reconnect = true;
      Id = nick;
    }
    #endregion

    #region properties/events
    /// <summary>
    /// Взвращает значение, характеризующее подключен ли клиент к серверу.
    /// </summary>
    public bool IsConnected { get { return handler == null ? false : handler.Connected; } }

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
    /// Ассиметричный алгоритм шифрования, данного соединения.
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

      if (handler != null && handler.Connected)
        throw new SocketException((int)SocketError.IsConnected);

      waitingAPIName = true;
      hostAddress = serverAddress;
      systemTimer = new Timer(SystemTimerCallback, null, SystemTimerInterval, -1);

      Socket connectingHandler = new Socket(serverAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      connectingHandler.BeginConnect(serverAddress, OnConnected, connectingHandler);
    }
    #endregion

    #region private/protected override methods
    private void OnConnected(IAsyncResult result)
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
        {
          ClientModel.OnConnected(this, new ConnectEventArgs { Error = se });
          ClientModel.Logger.Write(se);
        }
      }
      catch (Exception e)
      {
        ClientModel.OnConnected(this, new ConnectEventArgs { Error = e });
        ClientModel.Logger.Write(e);
      }
    }

    protected override void OnDataReceived(DataReceivedEventArgs e)
    {
      try
      {
        if (e.Error != null)
          throw e.Error;

        if (TrySetAPI(e))
          return;

        IClientAPICommand command = ClientModel.API.GetCommand(e.ReceivedData);
        command.Run(new ClientCommandArgs { Message = e.ReceivedData });
      }
      catch (Exception exc)
      {
        ClientModel.OnAsyncError(this, new AsyncErrorEventArgs { Error = exc });
        ClientModel.Logger.Write(exc);
      }
    }

    private bool TrySetAPI(DataReceivedEventArgs e)
    {
      if (!waitingAPIName)
        return false;

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
        throw new ModelException(ErrorCode.APINotSupported, serverAPIVersion);

      return true;
    }

    protected override void OnDataSended(DataSendedEventArgs args)
    {
      if (args.Error == null)
        return;

      ClientModel.OnAsyncError(this, new AsyncErrorEventArgs { Error = args.Error });
      ClientModel.Logger.Write(args.Error);
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
      if (handler != null && IsConnected)
      {
        if ((DateTime.Now - lastPingRequest).TotalMilliseconds >= PingInterval)
        {
          SendMessage(ServerPingRequestCommand.Id, null); // TODO: this should be moved in API

          lastPingRequest = DateTime.Now;
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

      lock (timerSync)
        if (systemTimer != null)
          systemTimer.Change(SystemTimerInterval, -1);
    }
    #endregion

    #region IDisposable

    public override void Dispose()
    {
      lock (timerSync)
      {
        base.Dispose();

        keyCryptor.Clear();

        if (systemTimer != null)
          systemTimer.Dispose();

        systemTimer = null;
      }
    }

    #endregion
  }
}
