using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Media;
using TCPChat.Engine.Connections;

namespace TCPChat.Engine.API.StandartAPI
{
    //Команды для клиента: (формат сообщений XX XX Serialized(this.MessageContent))
    //Расшифровка XX XX:
    //80 00: Регистрация принята
    //80 01: Регистрация не принята (ник уже существует)

    //80 10: Вывести общее сообщение для комнаты
    //80 11: Вывести личное сообщение
    //80 12: Вывести сообщение с файлом
    //80 13: Вывести системное сообщение

    //80 20: Получен откртый ключ пользователя

    //80 30: Открыта комната
    //80 31: Закрыта комната
    //80 32: Комната обновлена

    //80 40: Запрос части файла
    //80 41: Дописать часть файла
    //80 42: Файл больше не раздается
    //80 43: Прекращение загрузки файла

    //80 FF: Пинг ответ

    //FF FF: Пустая команда

    enum ClientCommand : ushort
    {
        RegistrationResponse = 0x8000,

        OutRoomMessage = 0x8010,
        OutPrivateMessage = 0x8011,
        OutFileMessage = 0x8012,
        OutSystemMessage = 0x8013,

        ReceiveUserOpenKey = 0x8020,

        RoomOpened = 0x8030,
        RoomClosed = 0x8031,
        RoomRefreshed = 0x8032,

        ReadFilePart = 0x8040,
        WriteFilePart = 0x8041,
        PostedFileDeleted = 0x8042,
        CancelDownloading = 0x8043,

        PingResponce = 0x80FF,

        Empty = 0xFFFF
    }

    class BaseClientCommand
    {
        protected static T GetContentFormMessage<T>(byte[] message)
        {
            MemoryStream messageStream = new MemoryStream(message);
            messageStream.Position = sizeof(ushort);
            BinaryFormatter formatter = new BinaryFormatter();
            T receivedContent = (T)formatter.Deserialize(messageStream);
            messageStream.Dispose();
            return receivedContent;
        }
    }

    class ClientRegistrationResponseCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        private EventHandler<RegistrationEventArgs> callback;

        public ClientRegistrationResponseCommand(EventHandler<RegistrationEventArgs> commandCallback)
        {
            callback = commandCallback;
        }

        public void Run(ClientCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (!receivedContent.Registered)
                args.API.Client.Dispose();

            EventHandler<RegistrationEventArgs> temp = Interlocked.CompareExchange<EventHandler<RegistrationEventArgs>>(ref callback, null, null);
            if (temp != null)
                temp(args.API.Client, new RegistrationEventArgs() { Registered = receivedContent.Registered });
        }

        [Serializable]
        public class MessageContent
        {
            bool registered;

            public bool Registered { get { return registered; } set { registered = value; } }
        }

        public const ushort Id = (ushort)ClientCommand.RegistrationResponse;
    }

    class ClientOutRoomMessageCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        private EventHandler<ReceiveMessageEventArgs> callback;

        public ClientOutRoomMessageCommand(EventHandler<ReceiveMessageEventArgs> commandCallback)
        {
            callback = commandCallback;
        }

        public void Run(ClientCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            ReceiveMessageEventArgs receiveMessageArgs = new ReceiveMessageEventArgs();
            receiveMessageArgs.IsFileMessage = false;
            receiveMessageArgs.IsPrivateMessage = false;
            receiveMessageArgs.IsSystemMessage = false;
            receiveMessageArgs.Message = receivedContent.Message;
            receiveMessageArgs.Sender = receivedContent.Sender;
            receiveMessageArgs.RoomName = receivedContent.RoomName;

            EventHandler<ReceiveMessageEventArgs> temp = Interlocked.CompareExchange<EventHandler<ReceiveMessageEventArgs>>(ref callback, null, null);
            if (temp != null)
                temp(args.API.Client, receiveMessageArgs);
        }

        [Serializable]
        public class MessageContent
        {
            private string sender;
            private string message;
            private string roomName;

            public string Sender { get { return sender; } set { sender = value; } }
            public string Message { get { return message; } set { message = value; } }
            public string RoomName { get { return roomName; } set { roomName = value; } }
        }

        public const ushort Id = (ushort)ClientCommand.OutRoomMessage;
    }

    class ClientOutPrivateMessageCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        private EventHandler<ReceiveMessageEventArgs> callback;

        public ClientOutPrivateMessageCommand(EventHandler<ReceiveMessageEventArgs> commandCallback)
        {
            callback = commandCallback;
        }

        public void Run(ClientCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            byte[] decryptedSymmetricKey = args.API.Client.KeyCryptor.Decrypt(receivedContent.Key, false);

            MemoryStream messageStream = new MemoryStream();
            MemoryStream encryptedMessageStream = new MemoryStream(receivedContent.Message);
            Crypter messageCrypter = new Crypter(new AesCryptoServiceProvider() { Padding = PaddingMode.Zeros, Mode = CipherMode.CBC });
            messageCrypter.DecryptStream(encryptedMessageStream, messageStream, decryptedSymmetricKey);
            messageCrypter.Dispose();

            ReceiveMessageEventArgs receiveMessageArgs = new ReceiveMessageEventArgs();
            receiveMessageArgs.IsFileMessage = false;
            receiveMessageArgs.IsPrivateMessage = true;
            receiveMessageArgs.IsSystemMessage = false;
            receiveMessageArgs.Message = Encoding.Unicode.GetString(messageStream.ToArray());
            receiveMessageArgs.Sender = receivedContent.Sender;

            encryptedMessageStream.Dispose();

            EventHandler<ReceiveMessageEventArgs> temp = Interlocked.CompareExchange<EventHandler<ReceiveMessageEventArgs>>(ref callback, null, null);
            if (temp != null)
                temp(args.API.Client, receiveMessageArgs);
        }

        [Serializable]
        public class MessageContent
        {
            byte[] key;
            byte[] message;
            string sender;

            public byte[] Key { get { return key; } set { key = value; } }
            public byte[] Message { get { return message; } set { message = value; } }
            public string Sender { get { return sender; } set { sender = value; } }
        }

        public const ushort Id = (ushort)ClientCommand.OutPrivateMessage;
    }

    class ClientReceiveUserOpenKeyCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        public void Run(ClientCommandArgs args)
        {
            StandartClientAPI API = (StandartClientAPI)args.API;
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            AwaitingPrivateMessage awaitingMessage = API.AwaitingPrivateMessages.Find((message) => message.Receiver.Equals(receivedContent.Nick));
            if (awaitingMessage == null) return;
            lock (API.AwaitingPrivateMessages)
                API.AwaitingPrivateMessages.Remove(awaitingMessage);

            ServerSendOneUserCommand.MessageContent sendingContent = new ServerSendOneUserCommand.MessageContent();
            sendingContent.Receiver = receivedContent.Nick;

            Crypter messageCrypter = new Crypter(new AesCryptoServiceProvider() { Padding = PaddingMode.Zeros, Mode = CipherMode.CBC });
            byte[] SymmetricKey = messageCrypter.GenerateKey();

            RSACryptoServiceProvider keyCryptor = new RSACryptoServiceProvider(ClientConnection.CryptorKeySize);
            keyCryptor.ImportParameters(receivedContent.OpenKey);
            sendingContent.Key = keyCryptor.Encrypt(SymmetricKey, false);
            keyCryptor.Clear();

            MemoryStream encryptedMessageStream = new MemoryStream();
            MemoryStream messageStream = new MemoryStream(Encoding.Unicode.GetBytes(awaitingMessage.Message));
            messageCrypter.EncryptStream(messageStream, encryptedMessageStream);
            sendingContent.Message = encryptedMessageStream.ToArray();

            encryptedMessageStream.Dispose();
            messageCrypter.Dispose();

            args.API.Client.SendAsync(ServerSendOneUserCommand.Id, sendingContent);
        }

        [Serializable]
        public class MessageContent
        {
            string nick;
            RSAParameters openKey;

            public string Nick { get { return nick; } set { nick = value; } }
            public RSAParameters OpenKey { get { return openKey; } set { openKey = value; } }
        }

        public const ushort Id = (ushort)ClientCommand.ReceiveUserOpenKey;
    }

    class ClientOutFileMessageCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        private EventHandler<ReceiveMessageEventArgs> callback;

        public ClientOutFileMessageCommand(EventHandler<ReceiveMessageEventArgs> commandCallback)
        {
            callback = commandCallback;
        }

        public void Run(ClientCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            ReceiveMessageEventArgs receiveMessageArgs = new ReceiveMessageEventArgs();
            receiveMessageArgs.IsFileMessage = true;
            receiveMessageArgs.IsPrivateMessage = false;
            receiveMessageArgs.IsSystemMessage = false;
            receiveMessageArgs.Message = receivedContent.File.Name;
            receiveMessageArgs.Sender = receivedContent.File.Owner.Nick;
            receiveMessageArgs.RoomName = receivedContent.RoomName;
            receiveMessageArgs.State = receivedContent.File;

            EventHandler<ReceiveMessageEventArgs> temp = Interlocked.CompareExchange<EventHandler<ReceiveMessageEventArgs>>(ref callback, null, null);
            if (temp != null)
                temp(args.API.Client, receiveMessageArgs);
        }

        [Serializable]
        public class MessageContent
        {
            FileDescription file;
            string roomName;

            public FileDescription File { get { return file; } set { file = value; } }
            public string RoomName { get { return roomName; } set { roomName = value; } }
        }

        public const ushort Id = (ushort)ClientCommand.OutFileMessage;
    }

    class ClientRoomOpenedCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        private EventHandler<RoomEventArgs> callback;

        public ClientRoomOpenedCommand(EventHandler<RoomEventArgs> commandCallback)
        {
            callback = commandCallback;
        }

        public void Run(ClientCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            EventHandler<RoomEventArgs> temp = Interlocked.CompareExchange<EventHandler<RoomEventArgs>>(ref callback, null, null);
            if (temp != null)
                temp(args.API.Client, new RoomEventArgs() { Room = receivedContent.Room });
        }

        [Serializable]
        public class MessageContent
        {
            RoomDescription room;

            public RoomDescription Room { get { return room; } set { room = value; } }
        }

        public const ushort Id = (ushort)ClientCommand.RoomOpened;
    }

    class ClientRoomClosedCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        private EventHandler<RoomEventArgs> callback;

        public ClientRoomClosedCommand(EventHandler<RoomEventArgs> commandCallback)
        {
            callback = commandCallback;
        }

        public void Run(ClientCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            EventHandler<RoomEventArgs> temp = Interlocked.CompareExchange<EventHandler<RoomEventArgs>>(ref callback, null, null);
            if (temp != null)
                temp(args.API.Client, new RoomEventArgs() { Room = receivedContent.Room });
        }

        [Serializable]
        public class MessageContent
        {
            RoomDescription room;

            public RoomDescription Room { get { return room; } set { room = value; } }
        }

        public const ushort Id = (ushort)ClientCommand.RoomClosed;
    }

    class ClientRoomRefreshedCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        private EventHandler<RoomEventArgs> callback;

        public ClientRoomRefreshedCommand(EventHandler<RoomEventArgs> commandCallback)
        {
            callback = commandCallback;
        }

        public void Run(ClientCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            EventHandler<RoomEventArgs> temp = Interlocked.CompareExchange<EventHandler<RoomEventArgs>>(ref callback, null, null);
            if (temp != null)
                temp(args.API.Client, new RoomEventArgs() { Room = receivedContent.Room });
        }

        [Serializable]
        public class MessageContent
        {
            RoomDescription room;

            public RoomDescription Room { get { return room; } set { room = value; } }
        }

        public const ushort Id = (ushort)ClientCommand.RoomRefreshed;
    }

    class ClientOutSystemMessageCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        private EventHandler<ReceiveMessageEventArgs> callback;

        public ClientOutSystemMessageCommand(EventHandler<ReceiveMessageEventArgs> commandCallback)
        {
            callback = commandCallback;
        }

        public void Run(ClientCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            ReceiveMessageEventArgs receiveMessageArgs = new ReceiveMessageEventArgs();
            receiveMessageArgs.IsFileMessage = false;
            receiveMessageArgs.IsPrivateMessage = false;
            receiveMessageArgs.IsSystemMessage = true;
            receiveMessageArgs.Message = receivedContent.Message;

            EventHandler<ReceiveMessageEventArgs> temp = Interlocked.CompareExchange<EventHandler<ReceiveMessageEventArgs>>(ref callback, null, null);
            if (temp != null)
                temp(args.API.Client, receiveMessageArgs);
        }

        [Serializable]
        public class MessageContent
        {
            string message;

            public string Message { get { return message; } set { message = value; } }
        }

        public const ushort Id = (ushort)ClientCommand.OutSystemMessage;
    }

    class ClientReadFilePartCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        public void Run(ClientCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (receivedContent.File == null)
                throw new ArgumentNullException("File");

            if (receivedContent.Length <= 0)
                throw new ArgumentException("Length <= 0");

            if (receivedContent.StartPartPosition < 0)
                throw new ArgumentException("StartPartPosition < 0");

            if (args.API.Client.PostedFiles.FirstOrDefault((current) => current.File.Equals(receivedContent.File)) == null)
            {
                ServerRemoveFileFormRoomCommand.MessageContent fileNotPostContent = new ServerRemoveFileFormRoomCommand.MessageContent();
                fileNotPostContent.File = receivedContent.File;
                fileNotPostContent.RoomName = receivedContent.RoomName;
                args.API.Client.SendAsync(ServerRemoveFileFormRoomCommand.Id, fileNotPostContent);
            }

            ServerFilePartResponceCommand.MessageContent sendingContent = new ServerFilePartResponceCommand.MessageContent();
            sendingContent.File = receivedContent.File;
            sendingContent.RequestID = receivedContent.RequestID;
            sendingContent.StartPartPosition = receivedContent.StartPartPosition;
            sendingContent.RoomName = receivedContent.RoomName;

            long partSize;
            if (receivedContent.File.Size < receivedContent.StartPartPosition + receivedContent.Length)
                partSize = receivedContent.File.Size - receivedContent.StartPartPosition;
            else
                partSize = receivedContent.Length;

            sendingContent.Part = new byte[partSize];

            FileStream sendingFileStream = args.API.Client.PostedFiles.First((current) => current.File.Equals(receivedContent.File)).ReadStream;
            sendingFileStream.Position = receivedContent.StartPartPosition;
            sendingFileStream.Read(sendingContent.Part, 0, sendingContent.Part.Length);

            args.API.Client.SendAsync(ServerFilePartResponceCommand.Id, sendingContent);
        }

        [Serializable]
        public class MessageContent
        {
            FileDescription file;
            long startPartPosition;
            long length;
            int requestID;
            string roomName;

            public FileDescription File { get { return file; } set { file = value; } }
            public long StartPartPosition { get { return startPartPosition; } set { startPartPosition = value; } }
            public long Length { get { return length; } set { length = value; } }
            public int RequestID { get { return requestID; } set { requestID = value; } }
            public string RoomName { get { return roomName; } set { roomName = value; } } 
        }

        public const ushort Id = (ushort)ClientCommand.ReadFilePart;
    }

    class ClientWriteFilePartCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        EventHandler<FileDownloadEventArgs> callback;

        public ClientWriteFilePartCommand(EventHandler<FileDownloadEventArgs> commandCallback)
        {
            callback = commandCallback;
        }

        private void OnDownload(FileDescription file, int progress, string roomName, ClientConnection client)
        {
            FileDownloadEventArgs downloadEventArgs = new FileDownloadEventArgs() { File = file, Progress = progress, RoomName = roomName};

            EventHandler<FileDownloadEventArgs> temp = Interlocked.CompareExchange<EventHandler<FileDownloadEventArgs>>(ref callback, null, null);
            if (temp != null)
                temp(client, downloadEventArgs);
        }

        public void Run(ClientCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            if (receivedContent.File == null)
                throw new ArgumentNullException("File");

            if (receivedContent.Part == null)
                throw new ArgumentNullException("Part");

            if (receivedContent.StartPartPosition < 0)
                throw new ArgumentException("StartPartPosition < 0");

            if (args.API.Client.DownloadingFiles.FirstOrDefault((current) => current.File.Equals(receivedContent.File)) == null)
                return;

            DownloadingFile downloadingFile = args.API.Client.DownloadingFiles.First((current) => current.File.Equals(receivedContent.File));

            if (downloadingFile.WriteStream.Position == receivedContent.StartPartPosition)
                downloadingFile.WriteStream.Write(receivedContent.Part, 0, receivedContent.Part.Length);

            if (downloadingFile.WriteStream.Position >= receivedContent.File.Size)
            {
                args.API.Client.DownloadingFiles.Remove(downloadingFile);
                downloadingFile.WriteStream.Dispose();

                OnDownload(receivedContent.File, 100, receivedContent.RoomName, args.API.Client);
            }
            else
            {
                ServerFilePartRequestCommand.MessageContent sendingContent = new ServerFilePartRequestCommand.MessageContent();
                sendingContent.File = receivedContent.File;
                sendingContent.Length = ClientConnection.DefaultFilePartSize;
                sendingContent.RoomName = receivedContent.RoomName;
                sendingContent.StartPartPosition = downloadingFile.WriteStream.Position;
                args.API.Client.SendAsync(ServerFilePartRequestCommand.Id, sendingContent);

                OnDownload(receivedContent.File, (int)((downloadingFile.WriteStream.Position * 100) / receivedContent.File.Size), receivedContent.RoomName, args.API.Client);
            }
        }

        [Serializable]
        public class MessageContent
        {
            FileDescription file;
            string roomName;
            long startPartPosition;
            byte[] part;

            public FileDescription File { get { return file; } set { file = value; } }
            public long StartPartPosition { get { return startPartPosition; } set { startPartPosition = value; } }
            public string RoomName { get { return roomName; } set { roomName = value; } } 
            public byte[] Part { get { return part; } set { part = value; } }
        }

        public const ushort Id = (ushort)ClientCommand.WriteFilePart;
    }

    class ClientPostedFileDeletedCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        EventHandler<FileDownloadEventArgs> callback;

        public ClientPostedFileDeletedCommand(EventHandler<FileDownloadEventArgs> commandCallback)
        {
            callback = commandCallback;
        }

        public void Run(ClientCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            EventHandler<FileDownloadEventArgs> temp = Interlocked.CompareExchange<EventHandler<FileDownloadEventArgs>>(ref callback, null, null);

            if (temp != null)
                temp(args.API.Client, new FileDownloadEventArgs() { File = receivedContent.File, Progress = 0, RoomName = receivedContent.RoomName });
        }

        [Serializable]
        public class MessageContent
        {
            FileDescription file;
            string roomName;

            public FileDescription File { get { return file; } set { file = value; } }
            public string RoomName { get { return roomName; } set { roomName = value; } } 
        }

        public const ushort Id = (ushort)ClientCommand.PostedFileDeleted;
    }

    class ClientCancelDownloadingCommand :
        BaseClientCommand,
        IClientAPICommand
    {
        public void Run(ClientCommandArgs args)
        {
            MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

            DownloadingFile downloadingFile = args.API.Client.DownloadingFiles.FirstOrDefault((current) => current.File.Equals(receivedContent.File));

            if (downloadingFile == null)
                return;

            args.API.Client.CancelDownloading(downloadingFile.File, false);
        }

        [Serializable]
        public class MessageContent
        {
            FileDescription file;
            string roomName;

            public FileDescription File { get { return file; } set { file = value; } }
            public string RoomName { get { return roomName; } set { roomName = value; } }
        }

        public const ushort Id = (ushort)ClientCommand.CancelDownloading;
    }

    class ClientPingResponceCommand : IClientAPICommand
    {
        public void Run(ClientCommandArgs args)
        {
            
        }

        public const ushort Id = (ushort)ClientCommand.PingResponce;
    }

    class ClientEmptyCommand : IClientAPICommand
    {
        public void Run(ClientCommandArgs args)
        {

        }

        public static readonly ClientEmptyCommand Empty = new ClientEmptyCommand();

        public const ushort Id = (ushort)ClientCommand.Empty;
    }
}
