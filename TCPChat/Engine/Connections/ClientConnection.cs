using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Linq;
using TCPChat.Engine.API;
using TCPChat.Engine.API.StandartAPI;

namespace TCPChat.Engine.Connections
{
    /// <summary>
    /// Клиентское соединение.
    /// </summary>
    public sealed class ClientConnection :
        Connection
    {
        #region consts
        public const int CryptorKeySize = 2048;
        public const long DefaultFilePartSize = 512 * 1024;
        private const int MaxReceivedDataSize = 1024 * 1024;
        private const int SystemTimerInterval = 1000;
        private const int ReconnectTimeInterval = 10 * 1000;
        private const int PingInterval = 1000;
        #endregion

        #region private fields
        private IClientAPI API;
        private IPEndPoint hostAddress;

        private RSACryptoServiceProvider keyCryptor;
        private SynchronizationContext GUIContext;

        private bool awaitingAPIName;
        private string serverAPIVersion;

        private int[] reconnectErrorsList;
        private bool reconnect;
        private bool reconnecting;
        private DateTime lastReconnect;
        private DateTime lastPingRequest;

        private List<DownloadingFile> downloadingFiles;
        private List<PostedFile> postedFiles;

        private Timer systemTimer;
        #endregion

        #region constructors
        /// <summary>
        /// Создает клиентское подключение к серверу.
        /// </summary>
        public ClientConnection(string nick)
        {
            keyCryptor = new RSACryptoServiceProvider(CryptorKeySize);
            GUIContext = SynchronizationContext.Current;
            downloadingFiles = new List<DownloadingFile>();
            postedFiles = new List<PostedFile>();
            awaitingAPIName = false;
            reconnecting = false;
            reconnect = true;
            reconnectErrorsList = new int[] { 10052, 10053, 10054, 10060, 10064 };

            info = new UserDescription(nick);
        }
        #endregion

        #region properties/events
        /// <summary>
        /// Взвращает значение, характеризующее подключен ли клиент к серверу.
        /// </summary>
        public bool Connected
        {
            get { return handler.Connected; }
        }

        /// <summary>
        /// Открытый ключ данного соединения.
        /// </summary>
        public RSAParameters OpenKey
        {
            get { return keyCryptor.ExportParameters(false); }
        }

        /// <summary>
        /// Асинхронный алгоритм шифрования, данного соединения.
        /// </summary>
        public RSACryptoServiceProvider KeyCryptor
        {
            get { return keyCryptor; }
        }

        /// <summary>
        /// Версия API используемая на сервере.
        /// </summary>
        public string ServerAPIVersion
        {
            get
            {
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
            get { return reconnect; }
            set { reconnect = value; }
        }

        /// <summary>
        /// Выложенные файлы.
        /// </summary>
        public List<PostedFile> PostedFiles
        {
            get { return postedFiles; }
        }

        /// <summary>
        /// Загружаемые файлы.
        /// </summary>
        public List<DownloadingFile> DownloadingFiles
        {
            get { return downloadingFiles; }
        }

        /// <summary>
        /// Событие происходит при обновлении списка подключенных к серверу клиентов.
        /// </summary>
        public event EventHandler<RoomEventArgs> RoomRefreshed;

        /// <summary>
        /// Событие происходит при подключении клиента к серверу.
        /// </summary>
        public event EventHandler<ConnectEventArgs> Connect;

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
        public void ConnectAsync(IPEndPoint serverAddress)
        {
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
        public void SendMessageAsync(string message, string roomName)
        {
            if (API == null)
                return;

            API.SendMessageAsync(message, roomName);
        }

        /// <summary>
        /// Асинхронно отправляет сообщение конкретному пользователю.
        /// </summary>
        /// <param name="receiver">Ник получателя.</param>
        /// <param name="message">Сообщение.</param>
        public void SendPrivateMessageAsync(string receiver, string message)
        {
            if (API == null)
                return;

            API.SendPrivateMessageAsync(receiver, message);
        }

        /// <summary>
        /// Асинхронно послыает запрос для регистрации на сервере.
        /// </summary>
        /// <param name="info">Ник, по которому будет совершена попытка подключения.</param>
        public void SendRegisterRequestAsync()
        {
            if (API == null)
                return;

            UserDescription userDescription = new UserDescription(info.Nick);
            userDescription.NickColor = info.NickColor;
            API.SendRegisterRequestAsync(userDescription, keyCryptor.ExportParameters(false));
        }

        /// <summary>
        /// Создает на сервере комнату.
        /// </summary>
        /// <param name="roomName">Название комнаты для создания.</param>
        public void CreateRoomAsync(string roomName)
        {
            if (API == null)
                return;

            API.CreateRoomAsync(roomName);
        }

        /// <summary>
        /// Удаляет комнату на сервере. Необходимо являться создателем комнаты.
        /// </summary>
        /// <param name="roomName">Название комнаты.</param>
        public void DeleteRoomAsync(string roomName)
        {
            if (API == null)
                return;

            API.DeleteRoomAsync(roomName);
        }

        /// <summary>
        /// Приглашает в комнату пользователей. Необходимо являться создателем комнаты.
        /// </summary>
        /// <param name="roomName">Название комнаты.</param>
        /// <param name="users">Перечисление пользователей, которые будут приглашены.</param>
        public void InviteUsersAsync(string roomName, IEnumerable<UserDescription> users)
        {
            if (API == null)
                return;

            API.InviteUsersAsync(roomName, users);
        }

        /// <summary>
        /// Удаляет пользователей из комнаты. Необходимо являться создателем комнаты.
        /// </summary>
        /// <param name="roomName">Название комнаты.</param>
        /// <param name="users">Перечисление пользователей, которые будут удалены из комнаты.</param>
        public void KickUsersAsync(string roomName, IEnumerable<UserDescription> users)
        {
            if (API == null)
                return;

            API.KickUsersAsync(roomName, users);
        }

        /// <summary>
        /// Осуществляет выход из комнаты пользователя.
        /// </summary>
        /// <param name="roomName">Название комнаты.</param>
        public void ExitFormRoomAsync(string roomName)
        {
            if (API == null)
                return;

            API.ExitFormRoomAsync(roomName);
        }

        /// <summary>
        /// Отправляет запрос о необходимости получения списка пользователей комнаты.
        /// </summary>
        /// <param name="roomName">Название комнтаы.</param>
        public void RefreshRoomAsync(string roomName)
        {
            if (API == null)
                return;

            API.RefreshRoomAsync(roomName);
        }

        /// <summary>
        /// Изменяет администратора комнаты.
        /// </summary>
        /// <param name="roomName">Название комнаты.</param>
        /// <param name="newAdmin">Пользователь назначаемый администратором.</param>
        public void SetRoomAdmin(string roomName, UserDescription newAdmin)
        {
            if (API == null)
                return;

            API.SetRoomAdmin(roomName, newAdmin);
        }

        /// <summary>
        /// Асинхронно посылает запрос для отмены регистрации на сервере.
        /// </summary>
        public void SendUnregisterRequestAsync()
        {
            if (API == null)
                return;

            API.SendUnregisterRequestAsync();
        }

        /// <summary>
        /// Добовляет файл на раздачу.
        /// </summary>
        /// <param name="roomName">Название комнаты в которую добавляется файл.</param>
        /// <param name="fileName">Путь к добовляемому файлу.</param>
        public void AddFileToRoomAsyc(string roomName, string fileName)
        {
            if (API == null)
                return;

            API.AddFileToRoomAsyc(roomName, fileName);
        }

        /// <summary>
        /// Удаляет файл с раздачи.
        /// </summary>
        /// <param name="roomName">Название комнаты из которой удаляется файл.</param>
        /// <param name="file">Описание удаляемого файла.</param>
        public void RemoveFileFromRoomAsyc(string roomName, FileDescription file)
        {
            if (API == null)
                return;

            API.RemoveFileFromRoomAsyc(roomName, file);
        }

        /// <summary>
        /// Загружает файл.
        /// </summary>
        /// <param name="path">Путь для сохранения файла.</param>
        /// <param name="roomName">Название комнаты где находится файл.</param>
        /// <param name="file">Описание файла.</param>
        public void DownloadFile(string path, string roomName, FileDescription file)
        {
            if (API == null)
                return;

            API.DownloadFile(path, roomName, file);
        }

        /// <summary>
        /// Останавлиает загрузку файла.
        /// </summary>
        /// <param name="file">Описание файла.</param>
        /// <param name="leaveLoadedPart">Если значение истино недогруженный файл не будет удалятся.</param>
        public void CancelDownloading(FileDescription file, bool leaveLoadedPart)
        {
            if (API == null)
                return;

            API.CancelDownloading(file, leaveLoadedPart);
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
                if (se.ErrorCode == 10061)
                    reconnecting = true;
                else
                    OnConnect(new ConnectEventArgs() { Error = se });
            }
            catch (Exception e)
            {
                OnConnect(new ConnectEventArgs() { Error = e });
            }
        }

        private void OnConnect(ConnectEventArgs args)
        {
            EventHandler<ConnectEventArgs> temp = Interlocked.CompareExchange<EventHandler<ConnectEventArgs>>(ref Connect, null, null);

            if (temp != null)
                GUIContext.Post(O => temp(this, args), null);
        }

        private void OnAsyncError(AsyncErrorEventArgs args)
        {
            EventHandler<AsyncErrorEventArgs> temp = Interlocked.CompareExchange<EventHandler<AsyncErrorEventArgs>>(ref AsyncError, null, null);

            if (temp != null)
                GUIContext.Post(O => temp(this, args), null);
        }

        private void OnSystemMessage(string message)
        {
            EventHandler<ReceiveMessageEventArgs> temp = Interlocked.CompareExchange<EventHandler<ReceiveMessageEventArgs>>(ref ReceiveMessage, null, null);

            if (temp != null)
                temp(this, new ReceiveMessageEventArgs()
                {
                    IsSystemMessage = true,
                    IsPrivateMessage = false,
                    Message = message
                });
        }

        private void SetStandartAPI()
        {
            API = new StandartClientAPI(this);
            API.AddCommand(ClientRegistrationResponseCommand.Id, new ClientRegistrationResponseCommand(ReceiveRegistrationResponse));
            API.AddCommand(ClientRoomRefreshedCommand.Id, new ClientRoomRefreshedCommand(RoomRefreshed));
            API.AddCommand(ClientOutRoomMessageCommand.Id, new ClientOutRoomMessageCommand(ReceiveMessage));
            API.AddCommand(ClientOutPrivateMessageCommand.Id, new ClientOutPrivateMessageCommand(ReceiveMessage));
            API.AddCommand(ClientOutSystemMessageCommand.Id, new ClientOutSystemMessageCommand(ReceiveMessage));
            API.AddCommand(ClientOutFileMessageCommand.Id, new ClientOutFileMessageCommand(ReceiveMessage));
            API.AddCommand(ClientReceiveUserOpenKeyCommand.Id, new ClientReceiveUserOpenKeyCommand());
            API.AddCommand(ClientRoomOpenedCommand.Id, new ClientRoomOpenedCommand(RoomOpened));
            API.AddCommand(ClientRoomClosedCommand.Id, new ClientRoomClosedCommand(RoomClosed));
            API.AddCommand(ClientReadFilePartCommand.Id, new ClientReadFilePartCommand());
            API.AddCommand(ClientWriteFilePartCommand.Id, new ClientWriteFilePartCommand(DownloadProgress));
            API.AddCommand(ClientPostedFileDeletedCommand.Id, new ClientPostedFileDeletedCommand(PostedFileDeleted));
            API.AddCommand(ClientCancelDownloadingCommand.Id, new ClientCancelDownloadingCommand());
            API.AddCommand(ClientPingResponceCommand.Id, new ClientPingResponceCommand());
        }

        protected override void ReleaseResource()
        {
            keyCryptor.Clear();

            if (systemTimer != null)
                systemTimer.Dispose();

            foreach (DownloadingFile current in downloadingFiles)
                current.Dispose();

            foreach (PostedFile current in postedFiles)
                current.Dispose();

            base.ReleaseResource();
        }

        protected override void OnDataReceived(DataReceivedEventArgs args)
        {
            try
            {
                if (args.Error != null)
                    throw args.Error;

                if (awaitingAPIName)
                {
                    serverAPIVersion = Encoding.Unicode.GetString(args.ReceivedData);

                    switch (serverAPIVersion)
                    {
                        case StandartServerAPI.API:
                            SetStandartAPI();
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

                IClientAPICommand command = API.GetCommand(args.ReceivedData);
                command.Run(new ClientCommandArgs() 
                { 
                    Message = args.ReceivedData,
                    API = API
                });
            }
            catch (Exception e)
            {
                OnAsyncError(new AsyncErrorEventArgs() { Error = e });
            }
        }

        private void SystemTimerCallback(object state)
        {
            if (handler != null && Connected)
            {
                if ((DateTime.Now - lastPingRequest).TotalMilliseconds >= PingInterval)
                {
                    SendAsync(ServerPingRequest.Id, null);

                    lastPingRequest = DateTime.Now;
                }
            }

            if (reconnecting)
            {
                if ((DateTime.Now - lastReconnect).TotalMilliseconds >= ReconnectTimeInterval)
                {
                    OnSystemMessage("Попытка соединения с сервером...");

                    if (handler != null)
                        handler.Close();

                    ConnectAsync(hostAddress);

                    lastReconnect = DateTime.Now;
                }
            }

            systemTimer.Change(SystemTimerInterval, -1);
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

            if (!reconnectErrorsList.Contains(se.ErrorCode))
                return false;

            reconnecting = true;
            lastReconnect = DateTime.Now;
            return true;
        }
        #endregion
    }
}
