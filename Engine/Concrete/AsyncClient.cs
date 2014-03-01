using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Engine.Concrete.API;
using Engine.Concrete.Connections;
using Engine.Concrete.API.StandartAPI;
using Engine.Abstract.Connections;
using Engine.Abstract;
using Engine.Concrete.Entities;
using Engine.Concrete.Containers;

namespace Engine.Concrete
{
  /// <summary>
  /// Клиентское соединение.
  /// </summary>
  public sealed class AsyncClient :
      Connection
  {
    #region consts
    public const int CryptorKeySize = 2048;
    public const long DefaultFilePartSize = 500 * 1024;
    private const int MaxReceivedDataSize = 1024 * 1024;
    private const int SystemTimerInterval = 1000;
    private const int ReconnectTimeInterval = 10 * 1000;
    private const int PingInterval = 3000;
    #endregion

    #region private fields
    private IClientAPI API;
    private IPEndPoint hostAddress;

    private RSACryptoServiceProvider keyCryptor;
    private SynchronizationContext GUIContext;

    private bool awaitingAPIName;
    private string serverAPIVersion;

    private SocketError[] reconnectErrorsList;
    private bool reconnect;
    private bool reconnecting;
    private DateTime lastReconnect;
    private DateTime lastPingRequest;

    private List<PeerConnection> peers;
    private List<WaitingCommandContainer> waitingCommands;

    private List<DownloadingFile> downloadingFiles;
    private List<PostedFile> postedFiles;

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
      downloadingFiles = new List<DownloadingFile>();
      postedFiles = new List<PostedFile>();
      peers = new List<PeerConnection>();
      waitingCommands = new List<WaitingCommandContainer>();
      awaitingAPIName = false;
      reconnecting = false;
      reconnect = true;
      reconnectErrorsList = new SocketError[] { SocketError.NetworkReset, SocketError.ConnectionAborted,
                                                      SocketError.ConnectionReset, SocketError.TimedOut,
                                                      SocketError.HostDown };

      info = new User(nick);
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

    /// <summary>
    /// Выложенные файлы.
    /// </summary>
    public List<PostedFile> PostedFiles
    {
      get
      {
        ThrowIfDisposed();
        return postedFiles;
      }
    }

    /// <summary>
    /// Загружаемые файлы.
    /// </summary>
    public List<DownloadingFile> DownloadingFiles
    {
      get
      {
        ThrowIfDisposed();
        return downloadingFiles;
      }
    }

    /// <summary>
    /// Подключенные напрямую пользователи.
    /// </summary>
    public List<PeerConnection> Peers
    {
      get
      {
        ThrowIfDisposed();
        return peers;
      }
    }

    /// <summary>
    /// Событие происходит при обновлении списка подключенных к серверу клиентов.
    /// </summary>
    public event EventHandler<RoomEventArgs> RoomRefreshed;

    /// <summary>
    /// Событие происходит при подключении клиента к серверу.
    /// </summary>
    public event EventHandler<ConnectEventArgs> Connected;

    /// <summary>
    /// Событие происходит при полученни ответа от сервера, о регистрации.
    /// </summary>
    public event EventHandler<RegistrationEventArgs> ReceiveRegistrationResponse;

    /// <summary>
    /// Событие происходит при полученнии сообщения от сервера.
    /// </summary>
    public event EventHandler<ReceiveMessageEventArgs> ReceiveMessage;

    /// <summary>
    /// Событие происходит при любой асинхронной ошибке.
    /// </summary>
    public event EventHandler<AsyncErrorEventArgs> AsyncError;

    /// <summary>
    /// Событие происходит при открытии комнаты клиентом. Или когда клиента пригласили в комнату.
    /// </summary>
    public event EventHandler<RoomEventArgs> RoomOpened;

    /// <summary>
    /// Событие происходит при закрытии комнаты клиентом, когда клиента кикают из комнаты.
    /// </summary>
    public event EventHandler<RoomEventArgs> RoomClosed;

    /// <summary>
    /// Событие происходит при получении части файла, а также при завершении загрузки файла.
    /// </summary>
    public event EventHandler<FileDownloadEventArgs> DownloadProgress;

    /// <summary>
    /// Происходит при удалении выложенного файла.
    /// </summary>
    public event EventHandler<FileDownloadEventArgs> PostedFileDeleted;
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
          throw new SocketException(10056);

      awaitingAPIName = true;
      hostAddress = serverAddress;
      systemTimer = new Timer(SystemTimerCallback, null, SystemTimerInterval, -1);
      Socket Handler = new Socket(serverAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      Handler.BeginConnect(serverAddress, ConnectCallback, Handler);
    }

    /// <summary>
    /// Асинхронно отправляет сообщение всем пользователям в комнате. Если клиента нет в комнате, сообщение игнорируется сервером.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    public void SendMessage(string message, string roomName)
    {
      ThrowIfDisposed();
      API.SendMessage(message, roomName);
    }

    /// <summary>
    /// Асинхронно отправляет сообщение конкретному пользователю.
    /// </summary>
    /// <param name="receiver">Ник получателя.</param>
    /// <param name="message">Сообщение.</param>
    public void SendPrivateMessage(string receiver, string message)
    {
      ThrowIfDisposed();
      if (API != null)
        API.SendPrivateMessage(receiver, message);
    }

    /// <summary>
    /// Асинхронно послыает запрос для регистрации на сервере.
    /// </summary>
    /// <param name="info">Ник, по которому будет совершена попытка подключения.</param>
    public void SendRegisterRequest()
    {
      ThrowIfDisposed();
      User userDescription = new User(info.Nick);
      userDescription.NickColor = info.NickColor;

      if (API != null)
        API.SendRegisterRequest(userDescription, keyCryptor.ExportParameters(false));
    }

    /// <summary>
    /// Создает на сервере комнату.
    /// </summary>
    /// <param name="roomName">Название комнаты для создания.</param>
    public void CreateRoom(string roomName)
    {
      ThrowIfDisposed();

      if (API != null)
        API.CreateRoom(roomName);
    }

    /// <summary>
    /// Удаляет комнату на сервере. Необходимо являться создателем комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    public void DeleteRoom(string roomName)
    {
      ThrowIfDisposed();

      if (API != null)
        API.DeleteRoom(roomName);
    }

    /// <summary>
    /// Приглашает в комнату пользователей. Необходимо являться создателем комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="users">Перечисление пользователей, которые будут приглашены.</param>
    public void InviteUsers(string roomName, IEnumerable<User> users)
    {
      ThrowIfDisposed();

      if (API != null)
        API.InviteUsers(roomName, users);
    }

    /// <summary>
    /// Удаляет пользователей из комнаты. Необходимо являться создателем комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="users">Перечисление пользователей, которые будут удалены из комнаты.</param>
    public void KickUsers(string roomName, IEnumerable<User> users)
    {
      ThrowIfDisposed();

      if (API != null)
        API.KickUsers(roomName, users);
    }

    /// <summary>
    /// Осуществляет выход из комнаты пользователя.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    public void ExitFormRoom(string roomName)
    {
      ThrowIfDisposed();

      if (API != null)
        API.ExitFormRoom(roomName);
    }

    /// <summary>
    /// Отправляет запрос о необходимости получения списка пользователей комнаты.
    /// </summary>
    /// <param name="roomName">Название комнтаы.</param>
    public void RefreshRoom(string roomName)
    {
      ThrowIfDisposed();

      if (API != null)
        API.RefreshRoom(roomName);
    }

    /// <summary>
    /// Изменяет администратора комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="newAdmin">Пользователь назначаемый администратором.</param>
    public void SetRoomAdmin(string roomName, User newAdmin)
    {
      ThrowIfDisposed();

      if (API != null)
        API.SetRoomAdmin(roomName, newAdmin);
    }

    /// <summary>
    /// Асинхронно посылает запрос для отмены регистрации на сервере.
    /// </summary>
    public void SendUnregisterRequest()
    {
      ThrowIfDisposed();

      if (API != null)
        API.SendUnregisterRequest();
    }

    /// <summary>
    /// Добовляет файл на раздачу.
    /// </summary>
    /// <param name="roomName">Название комнаты в которую добавляется файл.</param>
    /// <param name="fileName">Путь к добовляемому файлу.</param>
    public void AddFileToRoom(string roomName, string fileName)
    {
      ThrowIfDisposed();

      if (API != null)
        API.AddFileToRoom(roomName, fileName);
    }

    /// <summary>
    /// Удаляет файл с раздачи.
    /// </summary>
    /// <param name="roomName">Название комнаты из которой удаляется файл.</param>
    /// <param name="file">Описание удаляемого файла.</param>
    public void RemoveFileFromRoom(string roomName, FileDescription file)
    {
      ThrowIfDisposed();

      if (API != null)
        API.RemoveFileFromRoom(roomName, file);
    }

    /// <summary>
    /// Загружает файл.
    /// </summary>
    /// <param name="path">Путь для сохранения файла.</param>
    /// <param name="roomName">Название комнаты где находится файл.</param>
    /// <param name="file">Описание файла.</param>
    public void DownloadFile(string path, string roomName, FileDescription file)
    {
      ThrowIfDisposed();

      if (API != null)
        API.DownloadFile(path, roomName, file);
    }

    /// <summary>
    /// Останавлиает загрузку файла.
    /// </summary>
    /// <param name="file">Описание файла.</param>
    /// <param name="leaveLoadedPart">Если значение истино недогруженный файл не будет удалятся.</param>
    public void CancelDownloading(FileDescription file, bool leaveLoadedPart)
    {
      ThrowIfDisposed();

      if (API != null)
        API.CancelDownloading(file, leaveLoadedPart);
    }

    /// <summary>
    /// Асинхронно отправляет команду напрямую к пользователю. 
    /// Если подключение с пользователем еще не создано, оно будет создано.
    /// </summary>
    /// <param name="id">Индетификатор команды.</param>
    /// <param name="messageContent">Содержимое команды.</param>
    /// <param name="info">Пользователь которому следуюет отправить команду.</param>
    public void SendMessage(ushort id, object messageContent, User info)
    {
      ThrowIfDisposed();

      PeerConnection connection;
      lock (peers)
        connection = peers.FirstOrDefault((conn) => conn.Info.Equals(info));

      if (connection != null)
        connection.SendMessage(id, messageContent);
      else
      {
        lock (waitingCommands)
          waitingCommands.Add(new WaitingCommandContainer(info, id, messageContent));

        if (API != null)
          API.ConnectToPeer(info);
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
        peers.Add(peer);

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
          OnConnect(new ConnectEventArgs() { Error = se });
      }
      catch (Exception e)
      {
        OnConnect(new ConnectEventArgs() { Error = e });
      }
    }

    private void PeerConnected(object sender, ConnectEventArgs e)
    {
      if (e.Error != null)
      {
        OnAsyncError(new AsyncErrorEventArgs() { Error = e.Error });
        return;
      }

      PeerConnection peerConnection = (PeerConnection)sender;

      lock (waitingCommands)
      {
        IEnumerable<WaitingCommandContainer> commands = waitingCommands.Where((command) => command.Info.Equals(peerConnection.Info)).ToArray();

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
        OnAsyncError(new AsyncErrorEventArgs() { Error = e.Error });
        return;
      }

      PeerConnection peerConnection = (PeerConnection)sender;

      lock (peers)
      {
        if (peers.Contains(peerConnection))
          peers.Remove(peerConnection);

        peerConnection.Dispose();
      }
    }

    private void PeerAsyncError(object sender, AsyncErrorEventArgs e)
    {
      if (e.Error != null)
      {
        OnAsyncError(new AsyncErrorEventArgs() { Error = e.Error });
        return;
      }
    }

    private void PeerDataReceived(object sender, DataReceivedEventArgs e)
    {
      try
      {
        if (e.Error != null)
          throw e.Error;

        IClientAPICommand command = API.GetCommand(e.ReceivedData);
        ClientCommandArgs args = new ClientCommandArgs()
        {
          Message = e.ReceivedData,
          API = API,
          Peer = (PeerConnection)sender
        };

        command.Run(args);
      }
      catch (Exception exc)
      {
        OnAsyncError(new AsyncErrorEventArgs() { Error = exc });
      }
    }

    protected override void OnDataReceived(DataReceivedEventArgs e)
    {
      try
      {
        if (e.Error != null)
          throw e.Error;

        if (awaitingAPIName)
        {
          serverAPIVersion = Encoding.Unicode.GetString(e.ReceivedData);

          switch (serverAPIVersion)
          {
            case StandartServerAPI.API:
              API = new StandartClientAPI(this);
              break;
          }

          if (API != null)
          {
            OnConnect(new ConnectEventArgs() { Error = null });
            awaitingAPIName = false;
          }
          else
          {
            throw new APINotSupprtedException(serverAPIVersion);
          }
          return;
        }

        IClientAPICommand command = API.GetCommand(e.ReceivedData);
        ClientCommandArgs args = new ClientCommandArgs()
        {
          Message = e.ReceivedData,
          API = API
        };

        command.Run(args);
      }
      catch (Exception exc)
      {
        OnAsyncError(new AsyncErrorEventArgs() { Error = exc });
      }
    }

    protected override void ReleaseResource()
    {
      keyCryptor.Clear();

      if (systemTimer != null)
        systemTimer.Dispose();

      lock (downloadingFiles)
      {
        foreach (DownloadingFile current in downloadingFiles)
          current.Dispose();

        downloadingFiles.Clear();
      }

      lock (postedFiles)
      {
        foreach (PostedFile current in postedFiles)
          current.Dispose();

        postedFiles.Clear();
      }

      lock (peers)
      {
        foreach (PeerConnection current in peers)
          current.Dispose();

        peers.Clear();
      }

      base.ReleaseResource();
    }

    protected override void OnDataSended(DataSendedEventArgs args)
    {
      if (args.Error != null)
        OnAsyncError(new AsyncErrorEventArgs() { Error = args.Error });
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
            SendMessage(ServerPingRequest.Id, null);

            lastPingRequest = DateTime.Now;
          }

          lock (peers)
          {
            for (int i = peers.Count - 1; i >= 0; i--)
            {
              if (peers[i].IntervalOfSilence >= PeerConnection.ConnectionTimeOut)
              {
                peers[i].Dispose();
                peers.Remove(peers[i]);
              }
            }
          }
        }

        if (reconnecting)
        {
          if ((DateTime.Now - lastReconnect).TotalMilliseconds >= ReconnectTimeInterval)
          {
            OnSystemMessage("Попытка соединения с сервером...");

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

    internal void OnConnect(ConnectEventArgs args)
    {
      EventHandler<ConnectEventArgs> temp = Interlocked.CompareExchange(ref Connected, null, null);

      if (temp != null)
        GUIContext.Post(O => temp(this, args), null);
    }

    internal void OnAsyncError(AsyncErrorEventArgs args)
    {
      EventHandler<AsyncErrorEventArgs> temp = Interlocked.CompareExchange(ref AsyncError, null, null);

      if (temp != null)
        GUIContext.Post(O => temp(this, args), null);
    }

    internal void OnSystemMessage(string message)
    {
      EventHandler<ReceiveMessageEventArgs> temp = Interlocked.CompareExchange(ref ReceiveMessage, null, null);

      if (temp != null)
        GUIContext.Post(O => temp(this, new ReceiveMessageEventArgs()
        {
          Type = MessageType.System,
          Message = message
        }), null);
    }

    internal void OnReceiveMessage(ReceiveMessageEventArgs args)
    {
      EventHandler<ReceiveMessageEventArgs> temp = Interlocked.CompareExchange(ref ReceiveMessage, null, null);

      if (temp != null)
        GUIContext.Post(O => temp(this, args), null);
    }

    internal void OnRoomRefreshed(RoomEventArgs args)
    {
      EventHandler<RoomEventArgs> temp = Interlocked.CompareExchange(ref RoomRefreshed, null, null);

      if (temp != null)
        GUIContext.Post(O => temp(this, args), null);
    }

    internal void OnReceiveRegistrationResponse(RegistrationEventArgs args)
    {
      EventHandler<RegistrationEventArgs> temp = Interlocked.CompareExchange(ref ReceiveRegistrationResponse, null, null);

      if (temp != null)
        GUIContext.Post(O => temp(this, args), null);
    }

    internal void OnRoomOpened(RoomEventArgs args)
    {
      EventHandler<RoomEventArgs> temp = Interlocked.CompareExchange(ref RoomOpened, null, null);

      if (temp != null)
        GUIContext.Post(O => temp(this, args), null);
    }

    internal void OnRoomClosed(RoomEventArgs args)
    {
      EventHandler<RoomEventArgs> temp = Interlocked.CompareExchange(ref RoomClosed, null, null);

      if (temp != null)
        GUIContext.Post(O => temp(this, args), null);
    }

    internal void OnDownloadProgress(FileDownloadEventArgs args)
    {
      EventHandler<FileDownloadEventArgs> temp = Interlocked.CompareExchange(ref DownloadProgress, null, null);

      if (temp != null)
        GUIContext.Post(O => temp(this, args), null);
    }

    internal void OnPostedFileDeleted(FileDownloadEventArgs args)
    {
      EventHandler<FileDownloadEventArgs> temp = Interlocked.CompareExchange(ref PostedFileDeleted, null, null);

      if (temp != null)
        GUIContext.Post(O => temp(this, args), null);
    }
    #endregion
  }
}
