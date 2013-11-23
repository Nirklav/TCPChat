using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TCPChat.Engine;
using TCPChat.Engine.Connections;

namespace TCPChat.Engine.API.StandartAPI
{
    /// <summary>
    /// Класс реализующий стандартный API для клиента.
    /// </summary>
    class StandartClientAPI : IClientAPI
    {
        private Dictionary<ushort, IClientAPICommand> commandDictionary;

        /// <summary>
        /// Клинет являющийся хозяином данного API.
        /// </summary>
        public ClientConnection Client { get; private set; }

        /// <summary>
        /// Приватные сообщения которые ожидают открытого ключа, для шифрования.
        /// </summary>
        public List<AwaitingPrivateMessage> AwaitingPrivateMessages { get; private set; }

        /// <summary>
        /// Создает экземпляр API.
        /// </summary>
        /// <param name="host">Клиент которому будет принадлежать данный API.</param>
        public StandartClientAPI(ClientConnection host)
        {
            if (host == null)
                throw new ArgumentNullException("host");

            commandDictionary = new Dictionary<ushort, IClientAPICommand>();
            AwaitingPrivateMessages = new List<AwaitingPrivateMessage>();
            Client = host;
        }

        /// <summary>
        /// Добовляет команду в список команд.
        /// </summary>
        /// <param name="id">Id кманды.</param>
        /// <param name="command">Команда.</param>
        public void AddCommand(ushort id, IClientAPICommand command)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            commandDictionary.Add(id, command);
        }

        /// <summary>
        /// Извлекает команду.
        /// </summary>
        /// <param name="message">Пришедшее сообщение, по которому будет определена необходимая для извлекания команда.</param>
        /// <returns>Команда для выполнения.</returns>
        public IClientAPICommand GetCommand(byte[] message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            if (message.Length < 2)
                throw new ArgumentException("message.Length < 2");

            ushort id = BitConverter.ToUInt16(message, 0);

            try
            {
                return commandDictionary[id];
            }
            catch (KeyNotFoundException)
            {
                return ClientEmptyCommand.Empty;
            }
        }

        /// <summary>
        /// Асинхронно отправляет сообщение всем пользователям в комнате. Если клиента нет в комнате, сообщение игнорируется сервером.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        public void SendMessageAsync(string message, string roomName)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            if (string.IsNullOrEmpty(roomName))
                throw new ArgumentException("roomName");

            ServerSendRoomMessageCommand.MessageContent sendingContent = new ServerSendRoomMessageCommand.MessageContent();
            sendingContent.Message = message;
            sendingContent.RoomName = roomName;
            Client.SendAsync(ServerSendRoomMessageCommand.Id, sendingContent);
        }

        /// <summary>
        /// Асинхронно отправляет сообщение конкретному пользователю.
        /// </summary>
        /// <param name="receiver">Ник получателя.</param>
        /// <param name="message">Сообщение.</param>
        public void SendPrivateMessageAsync(string receiver, string message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            if (string.IsNullOrEmpty(receiver))
                throw new ArgumentException("receiver");

            AwaitingPrivateMessages.Add(new AwaitingPrivateMessage() { Receiver = receiver, Message = message });
            ServerSendUserOpenKeyCommand.MessageContent sendingContent = new ServerSendUserOpenKeyCommand.MessageContent();
            sendingContent.Nick = receiver;
            Client.SendAsync(ServerSendUserOpenKeyCommand.Id, sendingContent);
        }

        /// <summary>
        /// Асинхронно послыает запрос для регистрации на сервере.
        /// </summary>
        /// <param name="info">Информация о юзере, по которой будет совершена попытка подключения.</param>
        /// <param name="keyCryptor">Откртый ключ клиента.</param>
        public void SendRegisterRequestAsync(UserDescription info, RSAParameters openKey)
        {
            if (info== null)
                throw new ArgumentNullException("info");

            ServerRegisterCommand.MessageContent sendingContent = new ServerRegisterCommand.MessageContent();
            sendingContent.User = info;
            sendingContent.OpenKey = openKey;
            Client.SendAsync(ServerRegisterCommand.Id, sendingContent);
        }

        /// <summary>
        /// Асинхронно посылает запрос для отмены регистрации на сервере.
        /// </summary>
        public void SendUnregisterRequestAsync()
        {
            Client.SendAsync(ServerUnregisterCommand.Id, null);
        }

        /// <summary>
        /// Создает на сервере комнату.
        /// </summary>
        /// <param name="roomName">Название комнаты для создания.</param>
        public void CreateRoomAsync(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
                throw new ArgumentException("roomName");

            ServerCreateRoomCommand.MessageContent sendingContent = new ServerCreateRoomCommand.MessageContent() { RoomName = roomName };
            Client.SendAsync(ServerCreateRoomCommand.Id, sendingContent);
        }

        /// <summary>
        /// Удаляет комнату на сервере. Необходимо являться создателем комнаты.
        /// </summary>
        /// <param name="roomName">Название комнаты.</param>
        public void DeleteRoomAsync(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
                throw new ArgumentException("roomName");

            ServerDeleteRoomCommand.MessageContent sendingContent = new ServerDeleteRoomCommand.MessageContent() { RoomName = roomName };
            Client.SendAsync(ServerDeleteRoomCommand.Id, sendingContent);
        }

        /// <summary>
        /// Приглашает в комнату пользователей. Необходимо являться создателем комнаты.
        /// </summary>
        /// <param name="roomName">Название комнаты.</param>
        /// <param name="users">Перечисление пользователей, которые будут приглашены.</param>
        public void InviteUsersAsync(string roomName, IEnumerable<UserDescription> users)
        {
            if (string.IsNullOrEmpty(roomName))
                throw new ArgumentException("roomName");

            if (users == null)
                throw new ArgumentNullException("users");

            ServerInviteUsersCommand.MessageContent sendingContent = new ServerInviteUsersCommand.MessageContent() { RoomName = roomName, Users = users };
            Client.SendAsync(ServerInviteUsersCommand.Id, sendingContent);
        }

        /// <summary>
        /// Удаляет пользователей из комнаты. Необходимо являться создателем комнаты.
        /// </summary>
        /// <param name="roomName">Название комнаты.</param>
        /// <param name="users">Перечисление пользователей, которые будут удалены из комнаты.</param>
        public void KickUsersAsync(string roomName, IEnumerable<UserDescription> users)
        {
            if (string.IsNullOrEmpty(roomName))
                throw new ArgumentException("roomName");

            if (users == null)
                throw new ArgumentNullException("users");

            ServerKickUsersCommand.MessageContent sendingContent = new ServerKickUsersCommand.MessageContent() { RoomName = roomName, Users = users };
            Client.SendAsync(ServerKickUsersCommand.Id, sendingContent);
        }

        /// <summary>
        /// Осуществляет выход из комнаты пользователя.
        /// </summary>
        /// <param name="roomName">Название комнаты.</param>
        public void ExitFormRoomAsync(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
                throw new ArgumentException("roomName");

            ServerExitFormRoomCommand.MessageContent sendingContent = new ServerExitFormRoomCommand.MessageContent() { RoomName = roomName };
            Client.SendAsync(ServerExitFormRoomCommand.Id, sendingContent);
        }

        /// <summary>
        /// Отправляет запрос о необходимости получения списка пользователей комнаты.
        /// </summary>
        /// <param name="roomName">Название комнтаы.</param>
        public void RefreshRoomAsync(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
                throw new ArgumentException("roomName");

            ServerRefreshRoomCommand.MessageContent sendingContent = new ServerRefreshRoomCommand.MessageContent() { RoomName = roomName };
            Client.SendAsync(ServerRefreshRoomCommand.Id, sendingContent);
        }

        /// <summary>
        /// Изменяет администратора комнаты.
        /// </summary>
        /// <param name="roomName">Название комнаты.</param>
        /// <param name="newAdmin">Пользователь назначаемый администратором.</param>
        public void SetRoomAdmin(string roomName, UserDescription newAdmin)
        {
            if (string.IsNullOrEmpty(roomName))
                throw new ArgumentException("roomName");

            if (newAdmin == null)
                throw new ArgumentNullException("newAdmin");

            ServerSetRoomAdminCommand.MessageContent sendingContent = new ServerSetRoomAdminCommand.MessageContent() { RoomName = roomName, NewAdmin = newAdmin };
            Client.SendAsync(ServerSetRoomAdminCommand.Id, sendingContent);
        }

        /// <summary>
        /// Добовляет файл на раздачу.
        /// </summary>
        /// <param name="roomName">Название комнаты в которую добавляется файл.</param>
        /// <param name="fileName">Путь к добовляемому файлу.</param>
        public void AddFileToRoomAsyc(string roomName, string fileName)
        {
            if (string.IsNullOrEmpty(roomName))
                throw new ArgumentException("roomName");

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("fileName");

            FileInfo info = new FileInfo(fileName);

            if (!info.Exists)
                return;

            bool find = true;
            int id = 0;
            while (true)
            {
                find = true;

                foreach (PostedFile current in Client.PostedFiles)
                {
                    if (current.File.ID == id)
                    {
                        id++;
                        find = false;
                        break;
                    }
                }

                if (find)
                    break;
            }

            FileDescription file = new FileDescription(Client.Info, info.Length, Path.GetFileName(fileName), id);
            ServerAddFileToRoomCommand.MessageContent sendingContent = new ServerAddFileToRoomCommand.MessageContent() { RoomName = roomName, File = file };
            Client.SendAsync(ServerAddFileToRoomCommand.Id, sendingContent);
            Client.PostedFiles.Add(new PostedFile()
            {
                File = file,
                RoomName = roomName,
                ReadStream = new FileStream(fileName, FileMode.Open, FileAccess.Read)
            });
        }

        /// <summary>
        /// Удаляет файл с раздачи.
        /// </summary>
        /// <param name="roomName">Название комнаты из которой удаляется файл.</param>
        /// <param name="file">Описание удаляемого файла.</param>
        public void RemoveFileFromRoomAsyc(string roomName, FileDescription file)
        {
            if (string.IsNullOrEmpty(roomName))
                throw new ArgumentException("roomName");

            if (file == null)
                throw new ArgumentNullException("file");

            ServerRemoveFileFormRoomCommand.MessageContent sendingContent = new ServerRemoveFileFormRoomCommand.MessageContent() { RoomName = roomName, File = file };
            Client.SendAsync(ServerRemoveFileFormRoomCommand.Id, sendingContent);

            PostedFile postedFile = Client.PostedFiles.FirstOrDefault((current) => current.File.Equals(file));
            if (postedFile == null)
                return;

            Client.PostedFiles.Remove(postedFile);
        }

        /// <summary>
        /// Загружает файл.
        /// </summary>
        /// <param name="path">Путь для сохранения файла.</param>
        /// <param name="roomName">Название комнаты где находится файл.</param>
        /// <param name="file">Описание файла.</param>
        public void DownloadFile(string path, string roomName, FileDescription file)
        {
            if (string.IsNullOrEmpty(roomName))
                throw new ArgumentException("roomName");

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path");

            if (file == null)
                throw new ArgumentNullException("file");

            if (File.Exists(path))
                throw new ArgumentException("Файл уже существует");

            FileStream writeStream = File.Create(path);
            Client.DownloadingFiles.Add(new DownloadingFile() { File = file, WriteStream = writeStream });

            ServerFilePartRequestCommand.MessageContent sendingContent = new ServerFilePartRequestCommand.MessageContent();
            sendingContent.File = file;
            sendingContent.RoomName = roomName;
            sendingContent.StartPartPosition = 0;
            sendingContent.Length = ClientConnection.DefaultFilePartSize;
            Client.SendAsync(ServerFilePartRequestCommand.Id, sendingContent);
        }

        /// <summary>
        /// Останавлиает загрузку файла.
        /// </summary>
        /// <param name="file">Описание файла.</param>
        /// <param name="leaveLoadedPart">Если значение истино недогруженный файл не будет удалятся.</param>
        public void CancelDownloading(FileDescription file, bool leaveLoadedPart)
        {
            if (file == null)
                throw new ArgumentNullException("file");

            if (Client.DownloadingFiles.FirstOrDefault((current) => current.File.Equals(file)) == null)
                return;

            DownloadingFile downloadingFile = Client.DownloadingFiles.First((current) => current.File.Equals(file));
            string filePath = downloadingFile.WriteStream.Name;
            downloadingFile.Dispose();

            Client.DownloadingFiles.Remove(downloadingFile);

            if (!leaveLoadedPart)
                File.Delete(filePath);
        }
    }
}
