using System;
using System.Collections.Generic;
using System.Linq;
using TCPChat.Engine.Connections;

namespace TCPChat.Engine.API.StandartAPI
{
    /// <summary>
    /// Класс реазиующий стандартное серверное API.
    /// </summary>
    class StandartServerAPI : IServerAPI
    {
        /// <summary>
        /// Версия и имя данного API.
        /// </summary>
        public const string API = "StandartAPI v1.2";

        /// <summary>
        /// Сервер являющийся хозяином данного API.
        /// </summary>
        public AsyncServer Server { get; private set; }

        /// <summary>
        /// Список поданых запросов ожидающих ответа от клиентов.
        /// </summary>
        public Dictionary<int, ServerConnection> FilePartRequests { get; private set; }

        private Dictionary<ushort, IServerAPICommand> CommandDictionary = new Dictionary<ushort, IServerAPICommand>();

        /// <summary>
        /// Создает экземпляр API.
        /// </summary>
        /// <param name="host">Сервер которому будет принадлежать данный API.</param>
        public StandartServerAPI(AsyncServer host)
        {
            Server = host;
            FilePartRequests = new Dictionary<int, ServerConnection>();
        }

        /// <summary>
        /// Версия и имя данного API.
        /// </summary>
        public string APIName
        {
            get { return API; }
        }

        /// <summary>
        /// Добовляет команду в список команд.
        /// </summary>
        /// <param name="id">Id кманды.</param>
        /// <param name="command">Команда.</param>
        public void AddCommand(ushort id, IServerAPICommand command)
        {
            CommandDictionary.Add(id, command);
        }

        /// <summary>
        /// Извлекает команду.
        /// </summary>
        /// <param name="message">Пришедшее сообщение, по которому будет определена необходимая для извлекания команда.</param>
        /// <returns>Команда для выполнения.</returns>
        public IServerAPICommand GetCommand(byte[] message)
        {
            ushort id = BitConverter.ToUInt16(message, 0);

            try
            {
                return CommandDictionary[id];
            }
            catch(KeyNotFoundException)
            {
                return ServerEmptyCommand.Empty;
            }
        }

        /// <summary>
        /// Посылает системное сообщение клиенту.
        /// </summary>
        /// <param name="receiveConnection">Соединение которое получит сообщение.</param>
        /// <param name="message">Сообщение.</param>
        public void SendSystemMessage(ServerConnection receiveConnection, string message)
        {
            ClientOutSystemMessageCommand.MessageContent sendingContent = new ClientOutSystemMessageCommand.MessageContent() { Message = message };
            receiveConnection.SendAsync(ClientOutSystemMessageCommand.Id, sendingContent);
        }

        /// <summary>
        /// Закрывает соединение.
        /// </summary>
        /// <param name="nick">Ник пользователя, соединение котрого будет закрыто.</param>
        public void CloseConnection(string nick)
        {
            ServerConnection closingConnection = Server.Connections.Find((connection) => string.Equals(nick, connection.Info.Nick));

            if (closingConnection == null)
                return;

            CloseConnection(closingConnection);
        }

        /// <summary>
        /// Закрывает соединение.
        /// </summary>
        /// <param name="connection">Соединение которое будет закрыто.</param>
        public void CloseConnection(ServerConnection connection)
        {
            lock (Server.Connections)
            {
                Server.Connections.Remove(connection);

                foreach (string roomName in Server.Rooms.Keys)
                {
                    RoomDescription room = Server.Rooms[roomName];

                    if (!room.Users.Contains(connection.Info))
                        continue;

                    room.Users.Remove(connection.Info);

                    foreach (UserDescription user in room.Users)
                    {
                        if (user == null)
                            continue;

                        ServerConnection userConnection = Server.Connections.Find((conn) => user.Equals(conn.Info));
                        ClientRoomRefreshedCommand.MessageContent sendingContent = new ClientRoomRefreshedCommand.MessageContent() { Room = room };
                        userConnection.SendAsync(ClientRoomRefreshedCommand.Id, sendingContent);
                    }
                }
            }

            connection.Dispose();
        }
    }
}
