using Engine.Abstract;
using Engine.Concrete.Connections;
using Engine.Concrete.Containers;
using Engine.Concrete.Entities;
using Engine.Concrete.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Engine.Concrete.API.StandartAPI
{
  //Команды для клиента: (формат сообщений XX XX Serialized(this.MessageContent))
  //Расшифровка XX XX:
  //80 00: Регистрация принята
  //80 01: Регистрация не принята (ник уже существует)

  //80 10: Вывести общее сообщение для комнаты
  //80 11: Вывести личное сообщение
  //80 12: Вывести системное сообщение

  //80 20: Получен откртый ключ пользователя

  //80 30: Открыта комната
  //80 31: Закрыта комната
  //80 32: Комната обновлена

  //80 40: Опубликовать файл
  //80 41: Файл больше не раздается
  //80 42: Прочитать часть файла.
  //80 43: Записать часть файла.

  //80 50: Ожидать прямое соединение
  //80 51: Выполнить прямое соединение
  //80 52: Подключится к сервису P2P

  //80 FF: Пинг ответ

  //FF FF: Пустая команда

  enum ClientCommand : ushort
  {
    RegistrationResponse = 0x8000,

    OutRoomMessage = 0x8010,
    OutPrivateMessage = 0x8011,
    OutSystemMessage = 0x8013,

    ReceiveUserOpenKey = 0x8020,

    RoomOpened = 0x8030,
    RoomClosed = 0x8031,
    RoomRefreshed = 0x8032,

    FilePosted = 0x8040,
    PostedFileDeleted = 0x8041,
    ReadFilePart = 0x8042,
    WriteFilePart = 0x8043,

    WaitPeerConnection = 0x8050,
    ConnectToPeer = 0x8051,
    ConnectToP2PService = 0x8052,

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
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      args.API.Client.OnReceiveRegistrationResponse(new RegistrationEventArgs() { Registered = receivedContent.Registered });
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
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.Sender))
        throw new ArgumentException("sender");

      if (string.IsNullOrEmpty(receivedContent.Message))
        throw new ArgumentException("message");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("roomName");

      ReceiveMessageEventArgs receiveMessageArgs = new ReceiveMessageEventArgs();
      receiveMessageArgs.Type = MessageType.Common;
      receiveMessageArgs.Message = receivedContent.Message;
      receiveMessageArgs.Sender = receivedContent.Sender;
      receiveMessageArgs.RoomName = receivedContent.RoomName;

      args.API.Client.OnReceiveMessage(receiveMessageArgs);
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
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.Key == null)
        throw new ArgumentNullException("key");

      if (receivedContent.Message == null)
        throw new ArgumentNullException("message");

      if (string.IsNullOrEmpty(receivedContent.Sender))
        throw new ArgumentException("sender");

      byte[] decryptedSymmetricKey = args.API.Client.KeyCryptor.Decrypt(receivedContent.Key, false);

      MemoryStream messageStream = new MemoryStream();
      MemoryStream encryptedMessageStream = new MemoryStream(receivedContent.Message);
      Crypter messageCrypter = new Crypter(new AesCryptoServiceProvider() { Padding = PaddingMode.Zeros, Mode = CipherMode.CBC });
      messageCrypter.DecryptStream(encryptedMessageStream, messageStream, decryptedSymmetricKey);
      messageCrypter.Dispose();

      ReceiveMessageEventArgs receiveMessageArgs = new ReceiveMessageEventArgs();
      receiveMessageArgs.Type = MessageType.Private;
      receiveMessageArgs.Message = Encoding.Unicode.GetString(messageStream.ToArray());
      receiveMessageArgs.Sender = receivedContent.Sender;

      encryptedMessageStream.Dispose();

      args.API.Client.OnReceiveMessage(receiveMessageArgs);
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

      if (string.IsNullOrEmpty(receivedContent.Nick))
        throw new ArgumentException("Nick");

      WaitingPrivateMessage awaitingMessage = API.WaitingPrivateMessages.Find((message) => message.Receiver.Equals(receivedContent.Nick));
      if (awaitingMessage == null) return;
      lock (API.WaitingPrivateMessages)
        API.WaitingPrivateMessages.Remove(awaitingMessage);

      ServerSendOneUserCommand.MessageContent sendingContent = new ServerSendOneUserCommand.MessageContent();
      sendingContent.Receiver = receivedContent.Nick;

      Crypter messageCrypter = new Crypter(new AesCryptoServiceProvider() { Padding = PaddingMode.Zeros, Mode = CipherMode.CBC });
      byte[] SymmetricKey = messageCrypter.GenerateKey();

      RSACryptoServiceProvider keyCryptor = new RSACryptoServiceProvider(AsyncClient.CryptorKeySize);
      keyCryptor.ImportParameters(receivedContent.OpenKey);
      sendingContent.Key = keyCryptor.Encrypt(SymmetricKey, false);
      keyCryptor.Clear();

      MemoryStream encryptedMessageStream = new MemoryStream();
      MemoryStream messageStream = new MemoryStream(Encoding.Unicode.GetBytes(awaitingMessage.Message));
      messageCrypter.EncryptStream(messageStream, encryptedMessageStream);
      sendingContent.Message = encryptedMessageStream.ToArray();

      encryptedMessageStream.Dispose();
      messageCrypter.Dispose();

      args.API.Client.SendMessage(ServerSendOneUserCommand.Id, sendingContent);
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

  class ClientRoomOpenedCommand :
      BaseClientCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.Room == null)
        throw new ArgumentNullException("room");

      args.API.Client.OnRoomOpened(new RoomEventArgs() { Room = receivedContent.Room });
    }

    [Serializable]
    public class MessageContent
    {
      Room room;

      public Room Room { get { return room; } set { room = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.RoomOpened;
  }

  class ClientRoomClosedCommand :
      BaseClientCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.Room == null)
        throw new ArgumentNullException("room");

      args.API.Client.OnRoomClosed(new RoomEventArgs() { Room = receivedContent.Room });
    }

    [Serializable]
    public class MessageContent
    {
      Room room;

      public Room Room { get { return room; } set { room = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.RoomClosed;
  }

  class ClientRoomRefreshedCommand :
      BaseClientCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.Room == null)
        throw new ArgumentNullException("room");

      args.API.Client.OnRoomRefreshed(new RoomEventArgs() { Room = receivedContent.Room });
    }

    [Serializable]
    public class MessageContent
    {
      Room room;

      public Room Room { get { return room; } set { room = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.RoomRefreshed;
  }

  class ClientOutSystemMessageCommand :
      BaseClientCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.Message))
        throw new ArgumentException("message");

      ReceiveMessageEventArgs receiveMessageArgs = new ReceiveMessageEventArgs();
      receiveMessageArgs.Type = MessageType.System;
      receiveMessageArgs.Message = receivedContent.Message;

      args.API.Client.OnReceiveMessage(receiveMessageArgs);
    }

    [Serializable]
    public class MessageContent
    {
      string message;

      public string Message { get { return message; } set { message = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.OutSystemMessage;
  }

  class ClientFilePostedCommand :
      BaseClientCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.File == null)
        throw new ArgumentNullException("file");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("roomName");

      ReceiveMessageEventArgs receiveMessageArgs = new ReceiveMessageEventArgs();
      receiveMessageArgs.Type = MessageType.File;
      receiveMessageArgs.Message = receivedContent.File.Name;
      receiveMessageArgs.Sender = receivedContent.File.Owner.Nick;
      receiveMessageArgs.RoomName = receivedContent.RoomName;
      receiveMessageArgs.State = receivedContent.File;

      args.API.Client.OnReceiveMessage(receiveMessageArgs);
    }

    [Serializable]
    public class MessageContent
    {
      FileDescription file;
      string roomName;

      public FileDescription File { get { return file; } set { file = value; } }
      public string RoomName { get { return roomName; } set { roomName = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.FilePosted;
  }

  class ClientPostedFileDeletedCommand :
      BaseClientCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.File == null)
        throw new ArgumentNullException("file");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("roomName");

      lock (args.API.Client.DownloadingFiles)
      {
        IEnumerable<DownloadingFile> downloadFiles = args.API.Client.DownloadingFiles.Where((dFile) => dFile.File.Equals(receivedContent.File));

        foreach (DownloadingFile file in downloadFiles)
          file.Dispose();
      }

      FileDownloadEventArgs downloadEventArgs = new FileDownloadEventArgs();
      downloadEventArgs.File = receivedContent.File;
      downloadEventArgs.Progress = 0;
      downloadEventArgs.RoomName = receivedContent.RoomName;

      args.API.Client.OnPostedFileDeleted(downloadEventArgs);
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

  class ClientReadFilePartCommand :
      BaseClientCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      if (args.Peer == null)
        return;

      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.File == null)
        throw new ArgumentNullException("File");

      if (receivedContent.Length <= 0)
        throw new ArgumentException("Length <= 0");

      if (receivedContent.StartPartPosition < 0)
        throw new ArgumentException("StartPartPosition < 0");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("roomName");

      lock (args.API.Client.PostedFiles)
      {
        if (!args.API.Client.PostedFiles.Exists((current) => current.File.Equals(receivedContent.File)))
        {
          ServerRemoveFileFormRoomCommand.MessageContent fileNotPostContent = new ServerRemoveFileFormRoomCommand.MessageContent();
          fileNotPostContent.File = receivedContent.File;
          fileNotPostContent.RoomName = receivedContent.RoomName;
          args.API.Client.SendMessage(ServerRemoveFileFormRoomCommand.Id, fileNotPostContent);
          return;
        }
      }

      ClientWriteFilePartCommand.MessageContent sendingContent = new ClientWriteFilePartCommand.MessageContent();
      sendingContent.File = receivedContent.File;
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

      args.Peer.SendMessage(ClientWriteFilePartCommand.Id, sendingContent);
    }

    [Serializable]
    public class MessageContent
    {
      FileDescription file;
      long startPartPosition;
      long length;
      string roomName;

      public FileDescription File { get { return file; } set { file = value; } }
      public long StartPartPosition { get { return startPartPosition; } set { startPartPosition = value; } }
      public long Length { get { return length; } set { length = value; } }
      public string RoomName { get { return roomName; } set { roomName = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.ReadFilePart;
  }

  class ClientWriteFilePartCommand :
      BaseClientCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      if (args.Peer == null)
        return;

      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.File == null)
        throw new ArgumentNullException("File");

      if (receivedContent.Part == null)
        throw new ArgumentNullException("Part");

      if (receivedContent.StartPartPosition < 0)
        throw new ArgumentException("StartPartPosition < 0");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("roomName");

      DownloadingFile downloadingFile;
      lock (args.API.Client.DownloadingFiles)
        downloadingFile = args.API.Client.DownloadingFiles.FirstOrDefault((current) => current.File.Equals(receivedContent.File));

      if (downloadingFile == null)
        return;

      if (downloadingFile.WriteStream == null)
        downloadingFile.WriteStream = File.Create(downloadingFile.FullName);

      lock (downloadingFile.WriteStream)
      {
        if (downloadingFile.WriteStream.Position == receivedContent.StartPartPosition)
          downloadingFile.WriteStream.Write(receivedContent.Part, 0, receivedContent.Part.Length);
      }

      downloadingFile.File = receivedContent.File;
      FileDownloadEventArgs downloadEventArgs = new FileDownloadEventArgs();
      downloadEventArgs.RoomName = receivedContent.RoomName;
      downloadEventArgs.File = receivedContent.File;

      if (downloadingFile.WriteStream.Position >= receivedContent.File.Size)
      {
        lock (args.API.Client.DownloadingFiles)
          args.API.Client.DownloadingFiles.Remove(downloadingFile);

        downloadingFile.WriteStream.Dispose();

        downloadEventArgs.Progress = 100;
      }
      else
      {
        ClientReadFilePartCommand.MessageContent sendingContent = new ClientReadFilePartCommand.MessageContent();
        sendingContent.File = receivedContent.File;
        sendingContent.Length = AsyncClient.DefaultFilePartSize;
        sendingContent.RoomName = receivedContent.RoomName;
        sendingContent.StartPartPosition = downloadingFile.WriteStream.Position;
        args.Peer.SendMessage(ClientReadFilePartCommand.Id, sendingContent);

        downloadEventArgs.Progress = (int)((downloadingFile.WriteStream.Position * 100) / receivedContent.File.Size);
      }

      args.API.Client.OnDownloadProgress(downloadEventArgs);
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

  class ClientWaitPeerConnectionCommand :
      BaseClientCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.RemoteInfo == null)
        throw new ArgumentNullException("info");

      if (receivedContent.RequestPoint == null)
        throw new ArgumentNullException("request point");

      if (receivedContent.SenderPoint == null)
        throw new ArgumentNullException("sender point");

      PeerConnection connection;
      lock (args.API.Client.Peers)
        connection = args.API.Client.Peers.FirstOrDefault((conn) => !conn.ConnectedToPeer &&
                                                                    conn.ConnectId == receivedContent.ServiceConnectId);

      if (connection == null)
        throw new ArgumentException("empty peer connection do not found");

      connection.Disconnect();
      connection.Info = receivedContent.RemoteInfo;
      connection.WaitConnection(receivedContent.SenderPoint);

      ServerP2PConnectResponceCommand.MessageContent sendingContent = new ServerP2PConnectResponceCommand.MessageContent();
      sendingContent.PeerPoint = receivedContent.RequestPoint;
      sendingContent.ReceiverNick = receivedContent.RemoteInfo.Nick;
      sendingContent.RemoteInfo = args.API.Client.Info;
      sendingContent.ServiceConnectId = receivedContent.ServiceConnectId;
      args.API.Client.SendMessage(ServerP2PConnectResponceCommand.Id, sendingContent);
    }

    [Serializable]
    public class MessageContent
    {
      User remoteInfo;
      IPEndPoint senderPoint;
      IPEndPoint requestPoint;
      int serviceConnectId;

      public User RemoteInfo { get { return remoteInfo; } set { remoteInfo = value; } }
      public IPEndPoint SenderPoint { get { return senderPoint; } set { senderPoint = value; } }
      public IPEndPoint RequestPoint { get { return requestPoint; } set { requestPoint = value; } }
      public int ServiceConnectId { get { return serviceConnectId; } set { serviceConnectId = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.WaitPeerConnection;
  }

  class ClientConnectToPeerCommand :
      BaseClientCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.RemoteInfo == null)
        throw new ArgumentNullException("info");

      if (receivedContent.PeerPoint == null)
        throw new ArgumentNullException("PeerPoint");

      PeerConnection connection;
      lock (args.API.Client.Peers)
        connection = args.API.Client.Peers.FirstOrDefault((conn) => !conn.ConnectedToPeer &&
                                                                    conn.ConnectId == receivedContent.ServiceConnectId);

      if (connection == null)
        throw new ArgumentException("empty peer connection do not found");

      connection.Disconnect();
      connection.Info = receivedContent.RemoteInfo;
      connection.ConnectToPeer(receivedContent.PeerPoint);
    }

    [Serializable]
    public class MessageContent
    {
      int serviceConnectId;
      IPEndPoint peerPoint;
      User remoteInfo;

      public int ServiceConnectId { get { return serviceConnectId; } set { serviceConnectId = value; } }
      public IPEndPoint PeerPoint { get { return peerPoint; } set { peerPoint = value; } }
      public User RemoteInfo { get { return remoteInfo; } set { remoteInfo = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.ConnectToPeer;
  }

  class ClientConnectToP2PServiceCommand :
      BaseClientCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.ServicePoint == null)
        throw new ArgumentNullException("ServicePoint");

      PeerConnection connection = args.API.Client.CreatePeerConnection();

      connection.ConnectToService(receivedContent.ServicePoint, receivedContent.ServiceConnectId, receivedContent.Type);
    }

    [Serializable]
    public class MessageContent
    {
      IPEndPoint servicePoint;
      ConnectionType type;
      int serviceConnectId;

      public IPEndPoint ServicePoint { get { return servicePoint; } set { servicePoint = value; } }
      public ConnectionType Type { get { return type; } set { type = value; } }
      public int ServiceConnectId { get { return serviceConnectId; } set { serviceConnectId = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.ConnectToP2PService;
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
