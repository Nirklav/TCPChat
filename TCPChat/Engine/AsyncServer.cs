using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using TCPChat.Engine.API;
using TCPChat.Engine.API.StandartAPI;
using TCPChat.Engine.Connections;

namespace TCPChat.Engine
{
    class AsyncServer : IDisposable
    {
        #region const
        private const int ListenConnections = 100;
        private const int MaxDataSize = 2 * 1024 * 1024;
        private const int SystemTimerInterval = 1000;
        public const string MainRoomName = "Main room";
        #endregion

        #region private fields
        private Timer systemTimer;
        private bool isServerRunning;
        private Socket listener;
        private List<ServerConnection> connections;
        private Dictionary<string, RoomDescription> rooms;
        private Logger logger;
        private IServerAPI API;
        #endregion

        #region properties and events
        /// <summary>
        /// Возвращает true если сервер запущен.
        /// </summary>
        public bool IsServerRunning
        {
            get { return isServerRunning; }
        }

        /// <summary>
        /// Возвращает список соединений.
        /// </summary>
        public List<ServerConnection> Connections
        {
            get { return connections; }
        }

        /// <summary>
        /// Возвращает список комнат. И пользоваталей находящийхся в этих комнатах.
        /// </summary>
        public Dictionary<string, RoomDescription> Rooms
        {
            get { return rooms; }
        }
        #endregion

        #region constructors
        /// <summary>
        /// Констуктор сервера без файла для логирования.
        /// </summary>
        public AsyncServer() : this(null) {}

        /// <summary>
        /// Констуктор сервера с файлом для логирования.
        /// </summary>
        public AsyncServer(string LogFile)
        {
            connections = new List<ServerConnection>();
            rooms = new Dictionary<string, RoomDescription>();
            rooms.Add(MainRoomName, new RoomDescription(null, MainRoomName));
            isServerRunning = false;

            SetStandartAPI();

            if (!string.IsNullOrEmpty(LogFile))
                logger = new Logger(LogFile);
            else
                logger = null;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Включает сервер.
        /// </summary>
        /// <param name="ServerPort">Порт для соединение с сервером.</param>
        /// <param name="UsingIPv6">Использовать ли IPv6, при ложном значении будет использован IPv4.</param>
        public void Start(int ServerPort, bool UsingIPv6)
        {
            if (isServerRunning) return;

            isServerRunning = true;
            systemTimer = new System.Threading.Timer(SystemTimerCallback, null, 0, SystemTimerInterval);

            if (UsingIPv6)
            {
                listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.IPv6Any, ServerPort));
            }
            else
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Any, ServerPort));
            }
            
            listener.Listen(ListenConnections);
            listener.BeginAccept(AcceptCallback, null);
        }

        /// <summary>
        /// Устанавливает стандартное API для сервера. Для установки сервер должен быть выключен. Это API используется по умолчанию.
        /// </summary>
        public void SetStandartAPI()
        {
            if (isServerRunning)
                return;

            API = new StandartServerAPI(this);
            API.AddCommand(ServerRegisterCommand.Id, new ServerRegisterCommand());
            API.AddCommand(ServerUnregisterCommand.Id, new ServerUnregisterCommand());
            API.AddCommand(ServerSendRoomMessageCommand.Id, new ServerSendRoomMessageCommand());
            API.AddCommand(ServerSendOneUserCommand.Id, new ServerSendOneUserCommand());
            API.AddCommand(ServerSendUserOpenKeyCommand.Id, new ServerSendUserOpenKeyCommand());
            API.AddCommand(ServerCreateRoomCommand.Id, new ServerCreateRoomCommand());
            API.AddCommand(ServerDeleteRoomCommand.Id, new ServerDeleteRoomCommand());
            API.AddCommand(ServerInviteUsersCommand.Id, new ServerInviteUsersCommand());
            API.AddCommand(ServerKickUsersCommand.Id, new ServerKickUsersCommand());
            API.AddCommand(ServerExitFormRoomCommand.Id, new ServerExitFormRoomCommand());
            API.AddCommand(ServerRefreshRoomCommand.Id, new ServerRefreshRoomCommand());
            API.AddCommand(ServerSetRoomAdminCommand.Id, new ServerSetRoomAdminCommand());
            API.AddCommand(ServerAddFileToRoomCommand.Id, new ServerAddFileToRoomCommand());
            API.AddCommand(ServerRemoveFileFormRoomCommand.Id, new ServerRemoveFileFormRoomCommand());
            API.AddCommand(ServerFilePartRequestCommand.Id, new ServerFilePartRequestCommand());
            API.AddCommand(ServerFilePartResponceCommand.Id, new ServerFilePartResponceCommand());
            API.AddCommand(ServerPingRequest.Id, new ServerPingRequest());
        }
        #endregion

        #region private callback methods
        private void AcceptCallback(IAsyncResult result)
        {
            if (!isServerRunning) return;

            try
            {
                listener.BeginAccept(AcceptCallback, null);

                Socket handler = listener.EndAccept(result);
                ServerConnection connection = new ServerConnection(handler, MaxDataSize, logger, DataReceivedCallBack);
                connection.SendAPIName(API.APIName);

                lock(connections)
                    connections.Add(connection);
            }
            catch (Exception e)
            {
                if (logger != null)
                    logger.Write(e);

                return;
            }
        }

        private void DataReceivedCallBack(ServerConnection Sender, DataReceivedEventArgs args)
        {
            try
            {
                if (args.Error != null)
                    throw args.Error;

                IServerAPICommand command = API.GetCommand(args.ReceivedData);
                command.Run(new ServerCommandArgs() 
                { 
                    Message = args.ReceivedData,
                    UserConnection = Sender,
                    API = API
                });
            }
            catch (Exception e)
            {
                if (logger != null)
                    logger.Write(e);
            }
        }

        private void SystemTimerCallback(object arg)
        {
            for (int i = connections.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (connections[i].UnregisteredTimeInterval >= ServerConnection.UnregisteredTimeOut)
                    {
                        API.CloseConnection(connections[i]);
                        continue;
                    }

                    if (connections[i].IntervalOfSilence >= ServerConnection.ConnectionTimeOut)
                    {
                        API.CloseConnection(connections[i]);
                        continue;
                    }
                }
                catch (SocketException)
                {
                    API.CloseConnection(connections[i]);
                }
                catch (Exception e)
                {
                    if (logger != null)
                        logger.Write(e);
                }
            }

            IList<string> roomsNames = rooms.Keys.ToList();
            for (int i = rooms.Count - 1; i >= 0; i--)
            {
                if (string.Equals(roomsNames[i], AsyncServer.MainRoomName))
                    continue;

                if (rooms[roomsNames[i]].Users.Count == 0)
                    rooms.Remove(roomsNames[i]);
            }
        }
        #endregion

        #region IDisposable
        bool disposed = false;

        private void ReleaseManagedResource()
        {
            if (disposed) return;

            isServerRunning = false;

            lock (connections)
            {
                foreach (var connection in connections)
                {
                    connection.Dispose();
                }
                connections.Clear();
            }

            disposed = true;

            listener.Close();
            systemTimer.Dispose();
        }

        /// <summary>
        /// Особождает все ресуры используемые сервером.
        /// </summary>
        public void Dispose()
        {
            ReleaseManagedResource();
        }
        #endregion
    }
}
