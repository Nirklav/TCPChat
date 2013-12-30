using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using TCPChat.Engine.Connections;

namespace TCPChat.Engine.API.StandartAPI
{
    //Команды для сервера: (формат сообщений XX XX Serialized(this.MessageContent))
    //Расшифровка XX XX:
    //00 00: Запрос регистрации (в главной комнате)
    //00 01: Запрос выхода (из всех комнат)

    //00 10: Отправка сообщения всем клиентам в комнате
    //00 11: Отправка сообщения конкретному юзеру

    //00 20: Запрос открытого пароля пользователя

    //00 30: Создать комнату
    //00 31: Удалить комнату
    //00 32: Пригласить пользователей в комнату
    //00 33: Кикнуть пользователей из комнаты
    //00 34: Выйти из комнаты
    //00 35: Запрос обновления комнаты
    //00 36: Сделать пользователя администратором комнаты

    //00 40: Пинг запрос

    //00 50: Добавить файл на раздачу комнаты
    //00 51: Удалить файл с раздачи комнаты

    //00 60: Запрос прямого соединения
    //00 61: Ответ, говорящий о готовности принять входное содеинение

    //7F FF: Пустая команда

    enum ServerCommand : ushort
    {
        Register = 0x0000,
        Unregister = 0x0001,

        SendRoomMessage = 0x0010,
        SendOneUser = 0x0011,

        UserOpenKeyRequest = 0x0020,

        CreateRoom = 0x0030,
        DeleteRoom = 0x0031,
        InvateUsers = 0x0032,
        KickUsers = 0x0033,
        ExitFormRoom = 0x0034,
        RefreshRoom = 0x0035,
        SetRoomAdmin = 0x0036,

        PingRequest = 0x0040,

        AddFileToRoom = 0x0050,
        RemoveFileFromRoom = 0x0051,

        P2PConnectRequest = 0x0060,
        P2PConnectResponce = 0x0061,

        Empty = 0x7FFF
    }

    class BaseServerCommand
    {
        /// <summary>
        /// Извлекает сериализованную команду.
        /// </summary>
        /// <typeparam name="T">Тип сериализованых данных.</typeparam>
        /// <param name="message">Сериализованная команда.</param>
        /// <returns>Десериализованная команда.</returns>
        protected static T GetContentFormMessage<T>(byte[] message)
        {
            MemoryStream messageStream = new MemoryStream(message);
            messageStream.Position = sizeof(ushort);
            BinaryFormatter formatter = new BinaryFormatter();
            T receivedContent = (T)formatter.Deserialize(messageStream);
            messageStream.Dispose();
            return receivedContent;
        }

        /// <summary>
        /// Проверяет существует ли комната. Если нет отправляет вызвавщему команду соединению сообщение об ошибке. 
        /// А также команду закрытия комнаты.
        /// </summary>
        /// <param name="RoomName">Название комнаты.</param>
        /// <param name="args">Параметры команды.</param>
        /// <returns>Возвращает ложь если комнаты не существует.</returns>
        protected static bool RoomExists(string roomName, ServerCommandArgs args)
        {
            bool result;
            lock (args.API.Server.Rooms)
                result = args.API.Server.Rooms.ContainsKey(roomName);

            if (!result)
            {
                ClientRoomClosedCommand.MessageContent closeRoomContent = new ClientRoomClosedCommand.MessageContent() { Room = new RoomDescription(null, roomName) };
                args.UserConnection.SendMessage(ClientRoomClosedCommand.Id, closeRoomContent);
                args.API.SendSystemMessage(args.UserConnection, "На свервере нет комнаты с таким именем.");
            }

            return result;
        }
    }

    class ServerRegisterCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (receivedContent.User == null)
                throw new ArgumentNullException("User == null");

            bool newUserExist = false;
            lock (args.API.Server.Connections)
            {
                foreach (var ServerConnection in args.API.Server.Connections)
                {
                    if (!ServerConnection.IsRegistered)
                        continue;

                    if (receivedContent.User.Equals(ServerConnection.Info))
                    {
                        newUserExist = true;
                        break;
                    }
                }
            }

            if (!newUserExist)
            {
                RoomDescription room = args.API.Server.Rooms[AsyncServer.MainRoomName];

                lock (room.Users)
                    room.Users.Add(receivedContent.User);

                args.UserConnection.Register(receivedContent.User.Nick);
                args.UserConnection.OpenKey = receivedContent.OpenKey;
                args.UserConnection.Info.NickColor = receivedContent.User.NickColor;

                args.UserConnection.SendMessage(ClientRegistrationResponseCommand.Id, new ClientRegistrationResponseCommand.MessageContent() { Registered = !newUserExist });

                lock (args.API.Server.Connections)
                {
                    foreach (ServerConnection connection in args.API.Server.Connections)
                    {
                        ClientRoomRefreshedCommand.MessageContent sendingContent = new ClientRoomRefreshedCommand.MessageContent() { Room = args.API.Server.Rooms[AsyncServer.MainRoomName] };
                        connection.SendMessage(ClientRoomRefreshedCommand.Id, sendingContent);
                    }
                }
            }
            else
            {
                args.UserConnection.SendMessage(ClientRegistrationResponseCommand.Id, new ClientRegistrationResponseCommand.MessageContent() { Registered = !newUserExist });
                args.API.CloseConnection(args.UserConnection);
            }
        }

        [Serializable]
        public class MessageContent
        {
            RSAParameters openKey;
            UserDescription user;

            public RSAParameters OpenKey { get { return openKey; } set { openKey = value; } }
            public UserDescription User { get { return user; } set { user = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.Register;
    }

    class ServerUnregisterCommand : IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            args.API.CloseConnection(args.UserConnection);
        }

        public const ushort Id = (ushort)ServerCommand.Unregister;
    }

    class ServerSendRoomMessageCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (string.IsNullOrEmpty(receivedContent.Message))
                throw new ArgumentException("Message");

            if (string.IsNullOrEmpty(receivedContent.RoomName))
                throw new ArgumentNullException("RoomName");

            ClientOutRoomMessageCommand.MessageContent sendingContent = new ClientOutRoomMessageCommand.MessageContent();
            sendingContent.Message = receivedContent.Message;
            sendingContent.RoomName = receivedContent.RoomName;
            sendingContent.Sender = args.UserConnection.Info.Nick;

            if (!RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            lock (room.Users)
            {
                if (!room.Users.Contains(args.UserConnection.Info))
                {
                    args.API.SendSystemMessage(args.UserConnection, "Вы не можете отправить сообщение, т.к. не входите в состав этой комнаты.");
                    return;
                }

                foreach (UserDescription user in room.Users)
                {
                    if (user == null)
                        continue;

                    ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));

                    if (userConnection.IsRegistered)
                        userConnection.SendMessage(ClientOutRoomMessageCommand.Id, sendingContent);
                }
            }
        }

        [Serializable]
        public class MessageContent
        {
            string message;
            string roomName;

            public string Message { get { return message; } set { message = value; } }
            public string RoomName { get { return roomName; } set { roomName = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.SendRoomMessage;
    }

    class ServerSendOneUserCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (receivedContent.Key == null)
                throw new ArgumentNullException("Key == null");

            if (receivedContent.Message == null)
                throw new ArgumentNullException("Message == null");

            if (string.IsNullOrEmpty(receivedContent.Receiver))
                throw new ArgumentException("Receiver");

            ServerConnection receiverConnection;
            lock (args.API.Server.Connections)
                receiverConnection = args.API.Server.Connections.FirstOrDefault((connection) => string.Equals(receivedContent.Receiver, connection.Info.Nick));

            if (receiverConnection == null)
            {
                args.API.SendSystemMessage(args.UserConnection, "Данного пользователя нет в сети.");
                return;
            }

            ClientOutPrivateMessageCommand.MessageContent sendingContent = new ClientOutPrivateMessageCommand.MessageContent();
            sendingContent.Key = receivedContent.Key;
            sendingContent.Message = receivedContent.Message;
            sendingContent.Sender = args.UserConnection.Info.Nick;

            receiverConnection.SendMessage(ClientOutPrivateMessageCommand.Id, sendingContent);
        }

        [Serializable]
        public class MessageContent
        {
            string receiver;
            byte[] key;
            byte[] message;

            public string Receiver { get { return receiver; } set { receiver = value; } }
            public byte[] Key { get { return key; } set { key = value; } }
            public byte[] Message { get { return message; } set { message = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.SendOneUser;
    }

    class ServerSendUserOpenKeyCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (string.IsNullOrEmpty(receivedContent.Nick))
                throw new ArgumentException("Nick");

            ServerConnection requestConnection;
            lock (args.API.Server.Connections)
                requestConnection = args.API.Server.Connections.FirstOrDefault((connection) => string.Equals(receivedContent.Nick, connection.Info.Nick));

            if (requestConnection == null)
            {
                args.API.SendSystemMessage(args.UserConnection, "Данного пользователя нет в сети.");
                return;
            }

            ClientReceiveUserOpenKeyCommand.MessageContent sendingContent = new ClientReceiveUserOpenKeyCommand.MessageContent();
            sendingContent.Nick = receivedContent.Nick;
            sendingContent.OpenKey = requestConnection.OpenKey;
            args.UserConnection.SendMessage(ClientReceiveUserOpenKeyCommand.Id, sendingContent);
        }

        [Serializable]
        public class MessageContent
        {
            string nick;

            public string Nick { get { return nick; } set { nick = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.UserOpenKeyRequest;
    }

    class ServerCreateRoomCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (string.IsNullOrEmpty(receivedContent.RoomName))
                throw new ArgumentNullException("RoomName");

            RoomDescription creatingRoom = null;
            lock (args.API.Server.Rooms)
            {
                if (args.API.Server.Rooms.ContainsKey(receivedContent.RoomName))
                {
                    args.API.SendSystemMessage(args.UserConnection, "Комната с таким именем уже создана, выберите другое имя.");
                    return;
                }

                creatingRoom = new RoomDescription(args.UserConnection.Info, receivedContent.RoomName);
                args.API.Server.Rooms.Add(receivedContent.RoomName, creatingRoom);
            }

            ClientRoomOpenedCommand.MessageContent sendingContent = new ClientRoomOpenedCommand.MessageContent();
            sendingContent.Room = creatingRoom;
            args.UserConnection.SendMessage(ClientRoomOpenedCommand.Id, sendingContent);
        }

        [Serializable]
        public class MessageContent
        {
            string roomName;

            public string RoomName { get { return roomName; } set { roomName = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.CreateRoom;
    }

    class ServerDeleteRoomCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (string.IsNullOrEmpty(receivedContent.RoomName))
                throw new ArgumentException("RoomName");

            if (string.Equals(receivedContent.RoomName, AsyncServer.MainRoomName))
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы не можете удалить основную комнату.");
                return;
            }

            if (!RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription deletingRoom = args.API.Server.Rooms[receivedContent.RoomName];

            if (!deletingRoom.Admin.Equals(args.UserConnection.Info))
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы не являетесь администратором комнаты. Операция отменена.");
                return;
            }

            lock (args.API.Server.Rooms)
                args.API.Server.Rooms.Remove(deletingRoom.Name);

            ClientRoomClosedCommand.MessageContent sendingContent = new ClientRoomClosedCommand.MessageContent() { Room = deletingRoom };

            lock (deletingRoom.Users)
            {
                foreach (UserDescription user in deletingRoom.Users)
                {
                    ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));
                    userConnection.SendMessage(ClientRoomClosedCommand.Id, sendingContent);
                }
            }
        }

        [Serializable]
        public class MessageContent
        {
            string roomName;

            public string RoomName { get { return roomName; } set { roomName = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.DeleteRoom;
    }

    class ServerInviteUsersCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (string.IsNullOrEmpty(receivedContent.RoomName))
                throw new ArgumentException("RoomName");

            if (receivedContent.Users == null)
                throw new ArgumentNullException("Users == null");

            if (string.Equals(receivedContent.RoomName, AsyncServer.MainRoomName))
            {
                args.API.SendSystemMessage(args.UserConnection, "Невозможно пригласить пользователей в основную комнату. Они и так все здесь.");
                return;
            }

            if (!RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            if (!room.Admin.Equals(args.UserConnection.Info))
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы не являетесь администратором комнаты. Операция отменена.");
                return;
            }

            ClientRoomOpenedCommand.MessageContent sendingContent = new ClientRoomOpenedCommand.MessageContent() { Room = room };

            List<UserDescription> invitedUsers = new List<UserDescription>();
            foreach (UserDescription user in receivedContent.Users)
            {
                if (room.Users.Contains(user))
                    continue;

                room.Users.Add(user);
                invitedUsers.Add(user);
            }

            lock (room.Users)
            {
                foreach (UserDescription user in room.Users)
                {
                    ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));

                    if (invitedUsers.Contains(user))
                        userConnection.SendMessage(ClientRoomOpenedCommand.Id, sendingContent);
                    else
                    {
                        ClientRoomRefreshedCommand.MessageContent roomRefreshContent = new ClientRoomRefreshedCommand.MessageContent() { Room = room };
                        userConnection.SendMessage(ClientRoomRefreshedCommand.Id, roomRefreshContent);
                    }
                }
            }
        }

        [Serializable]
        public class MessageContent
        {
            string roomName;
            IEnumerable<UserDescription> users;

            public string RoomName { get { return roomName; } set { roomName = value; } }
            public IEnumerable<UserDescription> Users { get { return users; } set { users = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.InvateUsers;
    }

    class ServerKickUsersCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (string.IsNullOrEmpty(receivedContent.RoomName))
                throw new ArgumentException("RoomName");

            if (receivedContent.Users == null)
                throw new ArgumentNullException("Users == null");

            if (string.Equals(receivedContent.RoomName, AsyncServer.MainRoomName))
            {
                args.API.SendSystemMessage(args.UserConnection, "Невозможно удалить пользователей из основной комнаты.");
                return;
            }

            if (!RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            if (!room.Admin.Equals(args.UserConnection.Info))
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы не являетесь администратором комнаты. Операция отменена.");
                return;
            }

            ClientRoomClosedCommand.MessageContent sendingContent = new ClientRoomClosedCommand.MessageContent() { Room = room };

            foreach (UserDescription user in receivedContent.Users)
            {
                lock (room.Users)
                    if (!room.Users.Contains(user))
                        continue;

                if (user.Equals(room.Admin))
                {
                    args.API.SendSystemMessage(args.UserConnection, "Невозможно удалить из комнаты администратора.");
                    continue;
                }

                lock (room.Users)
                    room.Users.Remove(user);

                ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));
                userConnection.SendMessage(ClientRoomClosedCommand.Id, sendingContent);
            }

            lock (room.Users)
            {
                foreach (UserDescription user in room.Users)
                {
                    ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));
                    ClientRoomRefreshedCommand.MessageContent roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent() { Room = room };
                    userConnection.SendMessage(ClientRoomRefreshedCommand.Id, roomRefreshedContent);
                }
            }
        }

        [Serializable]
        public class MessageContent
        {
            string roomName;
            IEnumerable<UserDescription> users;

            public string RoomName { get { return roomName; } set { roomName = value; } }
            public IEnumerable<UserDescription> Users { get { return users; } set { users = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.KickUsers;
    }

    class ServerExitFormRoomCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (string.IsNullOrEmpty(receivedContent.RoomName))
                throw new ArgumentException("RoomName");

            if (string.Equals(receivedContent.RoomName, AsyncServer.MainRoomName))
            {
                args.API.SendSystemMessage(args.UserConnection, "Невозможно выйти из основной комнаты.");
                return;
            }

            if (!RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            lock (room.Users)
            {
                if (!room.Users.Contains(args.UserConnection.Info))
                {
                    args.API.SendSystemMessage(args.UserConnection, "Вы и так не входите в состав этой комнаты.");
                    return;
                }

                room.Users.Remove(args.UserConnection.Info);
            }

            args.UserConnection.SendMessage(ClientRoomClosedCommand.Id, new ClientRoomClosedCommand.MessageContent() { Room = room });

            if (room.Admin.Equals(args.UserConnection.Info))
            {
                lock (room.Users)
                    room.Admin = room.Users.FirstOrDefault();

                if (room.Admin != null)
                {
                    ServerConnection connection = args.API.Server.Connections.First((conn) => conn.Info.Equals(room.Admin));
                    args.API.SendSystemMessage(connection, string.Format("Вы назначены администратором комнаты \"{0}\".", room.Name));
                }
            }

            lock (room.Users)
            {
                foreach (UserDescription user in room.Users)
                {
                    ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));
                    ClientRoomRefreshedCommand.MessageContent roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent() { Room = room };
                    userConnection.SendMessage(ClientRoomRefreshedCommand.Id, roomRefreshedContent);
                }
            }
        }

        [Serializable]
        public class MessageContent
        {
            string roomName;

            public string RoomName { get { return roomName; } set { roomName = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.ExitFormRoom;
    }

    class ServerRefreshRoomCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (string.IsNullOrEmpty(receivedContent.RoomName))
                throw new ArgumentException("RoomName");

            if (string.Equals(receivedContent.RoomName, AsyncServer.MainRoomName))
                return;

            if (!RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            lock (room.Users)
            {
                if (!room.Users.Contains(args.UserConnection.Info))
                {
                    args.API.SendSystemMessage(args.UserConnection, "Вы не входите в состав этой комнаты.");
                    return;
                }
            }

            ClientRoomRefreshedCommand.MessageContent roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent() { Room = room };
            args.UserConnection.SendMessage(ClientRoomRefreshedCommand.Id, roomRefreshedContent);
        }

        [Serializable]
        public class MessageContent
        {
            string roomName;

            public string RoomName { get { return roomName; } set { roomName = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.RefreshRoom;
    }

    class ServerSetRoomAdminCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (string.IsNullOrEmpty(receivedContent.RoomName))
                throw new ArgumentException("RoomName");

            if (receivedContent.NewAdmin == null)
                throw new ArgumentNullException("NewAdmin");

            if (string.Equals(receivedContent.RoomName, AsyncServer.MainRoomName))
            {
                args.API.SendSystemMessage(args.UserConnection, "Невозможно назначить администратора для главной комнаты.");
                return;
            }

            if (!RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            if (!room.Admin.Equals(args.UserConnection.Info))
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы не являетесь администратором комнаты. Операция отменена.");
                return;
            }

            room.Admin = receivedContent.NewAdmin;

            ServerConnection connection = args.API.Server.Connections.First((conn) => conn.Info.Equals(receivedContent.NewAdmin));
            args.API.SendSystemMessage(connection, string.Format("Вы назначены администратором комнаты {0}.", room.Name));
        }

        [Serializable]
        public class MessageContent
        {
            string roomName;
            UserDescription newAdmin;

            public string RoomName { get { return roomName; } set { roomName = value; } }
            public UserDescription NewAdmin { get { return newAdmin; } set { newAdmin = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.SetRoomAdmin;
    }

    class ServerAddFileToRoomCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (receivedContent.File == null)
                throw new ArgumentNullException("File");

            if (string.IsNullOrEmpty(receivedContent.RoomName))
                throw new ArgumentException("RoomName");

            if (!RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            lock (room.Users)
            {
                if (!room.Users.Contains(args.UserConnection.Info))
                {
                    args.API.SendSystemMessage(args.UserConnection, "Вы не входите в состав этой комнаты.");
                    return;
                }
            }

            lock (room.Files)
            {
                if (room.Files.FirstOrDefault((file) => file.Equals(receivedContent.File)) == null)
                {
                    room.Files.Add(receivedContent.File);
                }
            }

            lock (room.Users)
            {
                foreach (UserDescription user in room.Users)
                {
                    ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));
                    ClientFilePostedCommand.MessageContent sendingContent = new ClientFilePostedCommand.MessageContent();
                    sendingContent.File = receivedContent.File;
                    sendingContent.RoomName = receivedContent.RoomName;
                    userConnection.SendMessage(ClientFilePostedCommand.Id, sendingContent);
                }
            }
        }

        [Serializable]
        public class MessageContent
        {
            string roomName;
            FileDescription file;

            public string RoomName { get { return roomName; } set { roomName = value; } }
            public FileDescription File { get { return file; } set { file = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.AddFileToRoom;
    }

    class ServerRemoveFileFormRoomCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (receivedContent.File == null)
                throw new ArgumentNullException("File");

            if (string.IsNullOrEmpty(receivedContent.RoomName))
                throw new ArgumentException("RoomName");

            if (!RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            lock (room.Files)
                if (!room.Files.Exists((file) => file.Equals(receivedContent.File)))
                    return;

            lock (room.Users)
            {
                if (!room.Users.Contains(args.UserConnection.Info))
                {
                    args.API.SendSystemMessage(args.UserConnection, "Вы не входите в состав этой комнаты.");
                    return;
                }
            }

            bool access = false;
            if (room.Admin != null)
                access |= args.UserConnection.Info.Equals(room.Admin);
            access |= args.UserConnection.Info.Equals(receivedContent.File.Owner);
            if (!access)
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы не можете удалить данный файл. Не хватает прав.");
                return;
            }

            lock(room.Files)
                room.Files.Remove(receivedContent.File);

            args.API.SendSystemMessage(args.UserConnection, string.Format("Файл \"{0}\" удален с раздачи.", receivedContent.File.Name));

            lock (room.Users)
            {
                foreach (UserDescription current in room.Users)
                {
                    ServerConnection connection = args.API.Server.Connections.First((conn) => conn.Info.Equals(current));
                    ClientPostedFileDeletedCommand.MessageContent postedFileDeletedContent = new ClientPostedFileDeletedCommand.MessageContent();
                    postedFileDeletedContent.File = receivedContent.File;
                    postedFileDeletedContent.RoomName = room.Name;
                    connection.SendMessage(ClientPostedFileDeletedCommand.Id, postedFileDeletedContent);
                }
            }
        }

        [Serializable]
        public class MessageContent
        {
            string roomName;
            FileDescription file;

            public string RoomName { get { return roomName; } set { roomName = value; } }
            public FileDescription File { get { return file; } set { file = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.RemoveFileFromRoom;
    }

    class ServerP2PConnectRequestCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (receivedContent.Info == null)
                throw new ArgumentNullException("Info");

            ServerConnection requestConnection;
            lock (args.API.Server.Connections)
                requestConnection = args.API.Server.Connections.FirstOrDefault((conn) => conn.Info.Equals(receivedContent.Info));

            if (requestConnection == null)
            {
                args.API.SendSystemMessage(args.UserConnection, "Данного пользователя не существует.");
                return;
            }

            int id = args.API.Server.P2PService.WaitConnection(requestConnection, args.UserConnection);

            ClientConnectToP2PServiceCommand.MessageContent sendingContent = new ClientConnectToP2PServiceCommand.MessageContent();
            sendingContent.ServicePoint = new IPEndPoint(Connection.GetIPAddress(requestConnection.RemotePoint.AddressFamily), args.API.Server.P2PService.Port);
            sendingContent.ServiceConnectId = id;
            sendingContent.Type = ConnectionType.Request;
            requestConnection.SendMessage(ClientConnectToP2PServiceCommand.Id, sendingContent);

            sendingContent.Type = ConnectionType.Sender;
            args.UserConnection.SendMessage(ClientConnectToP2PServiceCommand.Id, sendingContent);
        }

        [Serializable]
        public class MessageContent
        {
            UserDescription info;

            public UserDescription Info { get { return info; } set { info = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.P2PConnectRequest;
    }

    class ServerP2PConnectResponceCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (receivedContent.RemoteInfo == null)
                throw new ArgumentNullException("Info");

            if (receivedContent.PeerPoint == null)
                throw new ArgumentNullException("PeerPoint");

            ServerConnection receiverConnection;
            lock(args.API.Server.Connections)
                receiverConnection = args.API.Server.Connections.FirstOrDefault((conn) => string.Equals(conn.Info.Nick, receivedContent.ReceiverNick));

            if (receiverConnection == null)
            {
                args.API.SendSystemMessage(args.UserConnection, "Данного пользователя не существует.");
                return;
            }

            ClientConnectToPeerCommand.MessageContent connectContent = new ClientConnectToPeerCommand.MessageContent();
            connectContent.PeerPoint = receivedContent.PeerPoint;
            connectContent.RemoteInfo = receivedContent.RemoteInfo;
            connectContent.ServiceConnectId = receivedContent.ServiceConnectId;
            receiverConnection.SendMessage(ClientConnectToPeerCommand.Id, connectContent);
        }

        [Serializable]
        public class MessageContent
        {
            int serviceConnectId;
            string receiverNick;
            IPEndPoint peerPoint;
            UserDescription remoteInfo;

            public int ServiceConnectId { get { return serviceConnectId; } set { serviceConnectId = value; } }
            public string ReceiverNick { get { return receiverNick; } set { receiverNick = value; } }
            public IPEndPoint PeerPoint { get { return peerPoint; } set { peerPoint = value; } }
            public UserDescription RemoteInfo { get { return remoteInfo; } set { remoteInfo = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.P2PConnectResponce;
    }

    class ServerPingRequest : IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            args.UserConnection.SendMessage(ClientPingResponceCommand.Id, null);
        }

        public const ushort Id = (ushort)ServerCommand.PingRequest;
    }

    class ServerEmptyCommand : IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {

        }

        public static readonly ServerEmptyCommand Empty = new ServerEmptyCommand();

        public const ushort Id = (ushort)ServerCommand.Empty;
    }
}
