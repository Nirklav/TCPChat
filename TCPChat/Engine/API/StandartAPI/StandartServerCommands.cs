using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Drawing;
using TCPChat.Engine;
using TCPChat.Engine.Connections;
using System.Collections.Generic;

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
    //00 52: Запрос части файла
    //00 53: Отправить часть файла

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
        FilePartRequest = 0x0052,
        FilePartResponce = 0x0053,

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
            bool result = !args.API.Server.Rooms.ContainsKey(roomName);

            if (result)
            {
                ClientRoomClosedCommand.MessageContent closeRoomContent = new ClientRoomClosedCommand.MessageContent() { Room = new RoomDescription(null, roomName) };
                args.UserConnection.SendAsync(ClientRoomClosedCommand.Id, closeRoomContent);
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

            if (!newUserExist)
            {
                args.API.Server.Rooms[AsyncServer.MainRoomName].Users.Add(receivedContent.User);

                args.UserConnection.Register(receivedContent.User.Nick);
                args.UserConnection.OpenKey = receivedContent.OpenKey;
                args.UserConnection.Info.NickColor = receivedContent.User.NickColor;

                args.UserConnection.SendAsync(ClientRegistrationResponseCommand.Id, new ClientRegistrationResponseCommand.MessageContent() { Registered = !newUserExist });

                foreach (ServerConnection connection in args.API.Server.Connections)
                {
                    ClientRoomRefreshedCommand.MessageContent sendingContent = new ClientRoomRefreshedCommand.MessageContent() { Room = args.API.Server.Rooms[AsyncServer.MainRoomName] };
                    connection.SendAsync(ClientRoomRefreshedCommand.Id, sendingContent);
                }
            }
            else
            {
                args.UserConnection.SendAsync(ClientRegistrationResponseCommand.Id, new ClientRegistrationResponseCommand.MessageContent() { Registered = !newUserExist });
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

            if (RoomExists(receivedContent.RoomName, args))
                return;

            if (!args.API.Server.Rooms[receivedContent.RoomName].Users.Contains(args.UserConnection.Info))
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы не можете отправить сообщение, т.к. не входите в состав этой комнаты.");
                return;
            }

            foreach (UserDescription user in args.API.Server.Rooms[receivedContent.RoomName].Users)
            {
                if (user == null)
                    continue;

                ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));

                if (userConnection.IsRegistered)
                    userConnection.SendAsync(ClientOutRoomMessageCommand.Id, sendingContent);
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

            ServerConnection ReceiverConnection = args.API.Server.Connections.Find((connection) => string.Equals(receivedContent.Receiver, connection.Info.Nick));
            ClientOutPrivateMessageCommand.MessageContent SendingContent = new ClientOutPrivateMessageCommand.MessageContent();
            SendingContent.Key = receivedContent.Key;
            SendingContent.Message = receivedContent.Message;
            SendingContent.Sender = args.UserConnection.Info.Nick;

            ReceiverConnection.SendAsync(ClientOutPrivateMessageCommand.Id, SendingContent);
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

            ClientReceiveUserOpenKeyCommand.MessageContent sendingContent = new ClientReceiveUserOpenKeyCommand.MessageContent();
            sendingContent.Nick = receivedContent.Nick;
            sendingContent.OpenKey = args.API.Server.Connections.Find((connection) => string.Equals(receivedContent.Nick, connection.Info.Nick)).OpenKey;
            args.UserConnection.SendAsync(ClientReceiveUserOpenKeyCommand.Id, sendingContent);
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

            if (args.API.Server.Rooms.ContainsKey(receivedContent.RoomName))
            {
                args.API.SendSystemMessage(args.UserConnection, "Комната с таким именем уже создана, выберите другое имя.");
                return;
            }

            ClientRoomOpenedCommand.MessageContent sendingContent = new ClientRoomOpenedCommand.MessageContent();
            sendingContent.Room = new RoomDescription(args.UserConnection.Info, receivedContent.RoomName);
            args.API.Server.Rooms.Add(receivedContent.RoomName, sendingContent.Room);
            args.UserConnection.SendAsync(ClientRoomOpenedCommand.Id, sendingContent);
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

            if (RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription deletingRoom = args.API.Server.Rooms[receivedContent.RoomName];

            if (!deletingRoom.Admin.Equals(args.UserConnection.Info))
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы не являетесь администратором комнаты. Операция отменена.");
                return;
            }

            args.API.Server.Rooms.Remove(deletingRoom.Name);
            ClientRoomClosedCommand.MessageContent sendingContent = new ClientRoomClosedCommand.MessageContent() { Room = deletingRoom };

            foreach (UserDescription user in deletingRoom.Users)
            {                  
                ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));
                userConnection.SendAsync(ClientRoomClosedCommand.Id, sendingContent);
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

            if (RoomExists(receivedContent.RoomName, args))
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

            foreach (UserDescription user in room.Users)
            {
                ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));

                if (invitedUsers.Contains(user))
                    userConnection.SendAsync(ClientRoomOpenedCommand.Id, sendingContent);
                else
                {
                    ClientRoomRefreshedCommand.MessageContent roomRefreshContent = new ClientRoomRefreshedCommand.MessageContent() { Room = room };
                    userConnection.SendAsync(ClientRoomRefreshedCommand.Id, roomRefreshContent);
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

            if (RoomExists(receivedContent.RoomName, args))
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
                if (!room.Users.Contains(user))
                    continue;

                if (user.Equals(room.Admin))
                {
                    args.API.SendSystemMessage(args.UserConnection, "Невозможно удалить из комнаты администратора.");
                    continue;
                }

                args.API.Server.Rooms[receivedContent.RoomName].Users.Remove(user);

                ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));
                userConnection.SendAsync(ClientRoomClosedCommand.Id, sendingContent);
            }

            foreach (UserDescription user in room.Users)
            {
                ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));
                ClientRoomRefreshedCommand.MessageContent roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent() { Room = room };
                userConnection.SendAsync(ClientRoomRefreshedCommand.Id, roomRefreshedContent);
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

            if (RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            if (!room.Users.Contains(args.UserConnection.Info))
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы и так не входите в состав этой комнаты.");
                return;
            }

            room.Users.Remove(args.UserConnection.Info);
            args.UserConnection.SendAsync(ClientRoomClosedCommand.Id, new ClientRoomClosedCommand.MessageContent() { Room = room });

            if (room.Admin.Equals(args.UserConnection.Info))
            {
                room.Admin = room.Users.FirstOrDefault();

                if (room.Admin != null)
                {
                    ServerConnection connection = args.API.Server.Connections.First((conn) => conn.Info.Equals(room.Admin));
                    args.API.SendSystemMessage(connection, string.Format("Вы назначены администратором комнаты \"{0}\".", room.Name));
                }
            }

            foreach (UserDescription user in room.Users)
            {
                ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));
                ClientRoomRefreshedCommand.MessageContent roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent() { Room = room };
                userConnection.SendAsync(ClientRoomRefreshedCommand.Id, roomRefreshedContent);
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

            if (RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            if (!room.Users.Contains(args.UserConnection.Info))
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы не входите в состав этой комнаты.");
                return;
            }

            ClientRoomRefreshedCommand.MessageContent roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent() { Room = room };
            args.UserConnection.SendAsync(ClientRoomRefreshedCommand.Id, roomRefreshedContent);
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

            if (RoomExists(receivedContent.RoomName, args))
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

            if (RoomExists(receivedContent.RoomName, args))
                return;

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            if (!room.Users.Contains(args.UserConnection.Info))
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы не входите в состав этой комнаты.");
                return;
            }

            if (args.API.Server.Rooms[receivedContent.RoomName].Files.FirstOrDefault((file) => file.Equals(receivedContent.File)) != null)
            {
                args.API.SendSystemMessage(args.UserConnection, "Пользователь уже раздает данный файл.");
                return;
            }

            room.Files.Add(receivedContent.File);

            foreach (UserDescription user in room.Users)
            {
                ServerConnection userConnection = args.API.Server.Connections.Find((conn) => user.Equals(conn.Info));
                ClientOutFileMessageCommand.MessageContent sendingContent = new ClientOutFileMessageCommand.MessageContent();
                sendingContent.File = receivedContent.File;
                sendingContent.RoomName = receivedContent.RoomName;
                userConnection.SendAsync(ClientOutFileMessageCommand.Id, sendingContent);
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

            if (RoomExists(receivedContent.RoomName, args))
                return;

            if (args.API.Server.Rooms[receivedContent.RoomName].Files.FirstOrDefault((file) => file.Equals(receivedContent.File)) == null)
            {
                args.API.SendSystemMessage(args.UserConnection, "Пользователь не раздает данный файл.");
                return;
            }

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            if (!room.Users.Contains(args.UserConnection.Info))
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы не входите в состав этой комнаты.");
                return;
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

            room.Files.Remove(receivedContent.File);

            args.API.SendSystemMessage(args.UserConnection, string.Format("Файл \"{0}\" удален с раздачи.", receivedContent.File.Name));

            foreach (UserDescription current in room.Users)
            {
                ServerConnection connection = args.API.Server.Connections.First((conn) => conn.Info.Equals(current));
                ClientPostedFileDeletedCommand.MessageContent postedFileDeletedContent = new ClientPostedFileDeletedCommand.MessageContent();
                postedFileDeletedContent.File = receivedContent.File;
                postedFileDeletedContent.RoomName = room.Name;
                connection.SendAsync(ClientPostedFileDeletedCommand.Id, postedFileDeletedContent);
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

    class ServerFilePartRequestCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        private void CancelDownloading(string roomName, FileDescription file, ServerCommandArgs args)
        {
            ClientCancelDownloadingCommand.MessageContent content = new ClientCancelDownloadingCommand.MessageContent();
            content.File = file;
            content.RoomName = roomName;
            args.UserConnection.SendAsync(ClientCancelDownloadingCommand.Id, content);
        }

        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (receivedContent.File == null)
                throw new ArgumentNullException("File");

            if (receivedContent.Length <= 0)
                throw new ArgumentException("Length <= 0");

            if (string.IsNullOrEmpty(receivedContent.RoomName))
                throw new ArgumentException("RoomName");

            if (receivedContent.StartPartPosition < 0)
                throw new ArgumentException("StartFilePosition < 0");

            if (RoomExists(receivedContent.RoomName, args))
                return;

            if (args.API.Server.Rooms[receivedContent.RoomName].Files.FirstOrDefault((file) => file.Equals(receivedContent.File)) == null)
            {
                args.API.SendSystemMessage(args.UserConnection, "Пользователь уже не раздает данный файл. Загрузка прекращена.");
                CancelDownloading(receivedContent.RoomName, receivedContent.File, args);
                return;
            }

            RoomDescription room = args.API.Server.Rooms[receivedContent.RoomName];

            if (!room.Users.Contains(args.UserConnection.Info))
            {
                args.API.SendSystemMessage(args.UserConnection, "Вы не входите в состав этой комнаты. Загрузка прекращена.");
                CancelDownloading(receivedContent.RoomName, receivedContent.File, args);
                return;
            }

            int requestID = 0;
            StandartServerAPI serverAPI = (StandartServerAPI)args.API;
            lock (serverAPI.FilePartRequests)
            {
                while (serverAPI.FilePartRequests.ContainsKey(requestID))
                    requestID++;

                serverAPI.FilePartRequests.Add(requestID, args.UserConnection);
            }

            ClientReadFilePartCommand.MessageContent sendingContent = new ClientReadFilePartCommand.MessageContent();
            sendingContent.File = receivedContent.File;
            sendingContent.Length = receivedContent.Length;
            sendingContent.StartPartPosition = receivedContent.StartPartPosition;
            sendingContent.RequestID = requestID;
            sendingContent.RoomName = receivedContent.RoomName;

            ServerConnection fileOwnerConnection = args.API.Server.Connections.FirstOrDefault((conn) => conn.Info.Equals(receivedContent.File.Owner));
            if (fileOwnerConnection == null)
            {
                args.API.SendSystemMessage(args.UserConnection, "Владелец файла не в сети. Загрузка прекращена.");
                CancelDownloading(receivedContent.RoomName, receivedContent.File, args);
                return;
            }

            fileOwnerConnection.SendAsync(ClientReadFilePartCommand.Id, sendingContent);
        }

        [Serializable]
        public class MessageContent
        {
            long length;
            long startPartPosition;
            string roomName;
            FileDescription file;

            public string RoomName { get { return roomName; } set { roomName = value; } }
            public FileDescription File { get { return file; } set { file = value; } }
            public long Length { get { return length; } set { length = value; } }
            public long StartPartPosition { get { return startPartPosition; } set { startPartPosition = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.FilePartRequest;
    }

    class ServerFilePartResponceCommand :
        BaseServerCommand,
        IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (receivedContent.File == null)
                throw new ArgumentNullException("File");

            if (receivedContent.Part == null)
                throw new ArgumentNullException("Part");

            if (receivedContent.StartPartPosition < 0)
                throw new ArgumentException("StartFilePosition < 0");

            StandartServerAPI serverAPI = (StandartServerAPI)args.API;
            ServerConnection receiver = serverAPI.FilePartRequests[receivedContent.RequestID];
            lock (serverAPI.FilePartRequests)
                serverAPI.FilePartRequests.Remove(receivedContent.RequestID);

            ClientWriteFilePartCommand.MessageContent sendingContent = new ClientWriteFilePartCommand.MessageContent();
            sendingContent.File = receivedContent.File;
            sendingContent.Part = receivedContent.Part;
            sendingContent.RoomName = receivedContent.RoomName;
            sendingContent.StartPartPosition = receivedContent.StartPartPosition;
            receiver.SendAsync(ClientWriteFilePartCommand.Id, sendingContent);
        }

        [Serializable]
        public class MessageContent
        {
            int requestID;
            long startPartPosition;
            byte[] part;
            string roomName;
            FileDescription file;

            public long StartPartPosition { get { return startPartPosition; } set { startPartPosition = value; } }
            public string RoomName { get { return roomName; } set { roomName = value; } }
            public byte[] Part { get { return part; } set { part = value; } }
            public FileDescription File { get { return file; } set { file = value; } }
            public int RequestID { get { return requestID; } set { requestID = value; } }
        }

        public const ushort Id = (ushort)ServerCommand.FilePartResponce;
    }

    class ServerPingRequest : IServerAPICommand
    {
        public void Run(ServerCommandArgs args)
        {
            args.UserConnection.SendAsync(ClientPingResponceCommand.Id, null);
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
