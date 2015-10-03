using Engine.API.ClientCommands;
using Engine.API.ServerCommands;
using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Entities;
using Engine.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;

namespace Engine.API
{
  /// <summary>
  /// Класс реализующий стандартный API для клиента.
  /// </summary>
  sealed class StandardClientAPI :
    MarshalByRefObject,
    IClientAPI
  {
    internal class WaitingPrivateMessage
    {
      public string Receiver { get; set; }
      public string Message { get; set; }
    }

    private readonly List<WaitingPrivateMessage> waitingPrivateMessages;
    private readonly Dictionary<ushort, ICommand<ClientCommandArgs>> commands;
    private readonly Random idCreator;
    private long lastSendedNumber;

    /// <summary>
    /// Создает экземпляр API.
    /// </summary>
    /// <param name="host">Клиент которому будет принадлежать данный API.</param>
    [SecurityCritical]
    public StandardClientAPI()
    {
      commands = new Dictionary<ushort, ICommand<ClientCommandArgs>>();
      waitingPrivateMessages = new List<WaitingPrivateMessage>();
      idCreator = new Random(DateTime.Now.Millisecond);

      ClientModel.Recorder.Recorded += OnRecorded;

      AddCommand(new ClientRegistrationResponseCommand());
      AddCommand(new ClientRoomRefreshedCommand());
      AddCommand(new ClientOutRoomMessageCommand());
      AddCommand(new ClientOutPrivateMessageCommand());
      AddCommand(new ClientOutSystemMessageCommand());
      AddCommand(new ClientFilePostedCommand());
      AddCommand(new ClientReceiveUserOpenKeyCommand());
      AddCommand(new ClientRoomOpenedCommand());
      AddCommand(new ClientRoomClosedCommand());
      AddCommand(new ClientPostedFileDeletedCommand());
      AddCommand(new ClientReadFilePartCommand());
      AddCommand(new ClientWriteFilePartCommand());
      AddCommand(new ClientPingResponceCommand());
      AddCommand(new ClientConnectToPeerCommand());
      AddCommand(new ClientWaitPeerConnectionCommand());
      AddCommand(new ClientConnectToP2PServiceCommand());
      AddCommand(new ClientPlayVoiceCommand());
    }

    [SecurityCritical]
    private void AddCommand(ICommand<ClientCommandArgs> command)
    {
      commands.Add(command.Id, command);
    }

    [SecurityCritical]
    private void OnRecorded(object sender, RecordedEventArgs e)
    {
      if (!ClientModel.IsInited)
        return;

      var data = new byte[e.DataSize];
      Buffer.BlockCopy(e.Data, 0, data, 0, data.Length);

      var content = new ClientPlayVoiceCommand.MessageContent
      {
        Pack = new SoundPack
        {
          Data = data,
          Channels = e.Channels,
          BitPerChannel = e.BitPerChannel,
          Frequency = e.Frequency
        },
        Number = Interlocked.Increment(ref lastSendedNumber)
      };

      using (var client = ClientModel.Get())
      {
        var receivers = client.Rooms.Values
          .OfType<VoiceRoom>()
          .SelectMany(r => r.Users)
          .Distinct()
          .ToList();

        receivers.Remove(client.User.Nick);

        ClientModel.Peer.SendMessageIfConnected(receivers, ClientPlayVoiceCommand.CommandId, content, true);
      }
    }

    /// <summary>
    /// Извлекает команду.
    /// </summary>
    /// <param name="message">Пришедшее сообщение, по которому будет определена необходимая для извлекания команда.</param>
    /// <returns>Команда для выполнения.</returns>
    [SecuritySafeCritical]
    public ICommand<ClientCommandArgs> GetCommand(byte[] message)
    {
      if (message == null)
        throw new ArgumentNullException("message");

      if (message.Length < 2)
        throw new ArgumentException("message.Length < 2");

      var id = BitConverter.ToUInt16(message, 0);

      ICommand<ClientCommandArgs> command;
      if (commands.TryGetValue(id, out command))
        return command;

      if (ClientModel.Plugins.TryGetCommand(id, out command))
        return command;

      return ClientEmptyCommand.Empty;
    }

    /// <summary>
    /// Редактирует сообщение.
    /// </summary>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="roomName">Название комнаты.</param>
    [SecuritySafeCritical]
    public void SendMessage(long? messageId, string message, string roomName)
    {
      if (message == null)
        throw new ArgumentNullException("message");

      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      var sendingContent = new ServerSendRoomMessageCommand.MessageContent { Message = message, RoomName = roomName, MessageId = messageId };
      ClientModel.Client.SendMessage(ServerSendRoomMessageCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Асинхронно отправляет сообщение конкретному пользователю.
    /// </summary>
    /// <param name="receiver">Ник получателя.</param>
    /// <param name="message">Сообщение.</param>
    [SecuritySafeCritical]
    public void SendPrivateMessage(string receiver, string message)
    {
      if (message == null)
        throw new ArgumentNullException("message");

      if (string.IsNullOrEmpty(receiver))
        throw new ArgumentException("receiver");

      lock (waitingPrivateMessages)
        waitingPrivateMessages.Add(new WaitingPrivateMessage { Receiver = receiver, Message = message });

      var sendingContent = new ServerGetUserOpenKeyCommand.MessageContent { Nick = receiver };
      ClientModel.Client.SendMessage(ServerGetUserOpenKeyCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Возвращает ожидающее отправки сообщение.
    /// </summary>
    /// <param name="receiver">Имя получателя.</param>
    /// <returns>Ожидающее отправки сообщение.</returns>
    [SecuritySafeCritical]
    internal WaitingPrivateMessage GetWaitingMessage(string receiver)
    {
      lock (waitingPrivateMessages)
      {
        var waitingMessage = waitingPrivateMessages.Find(m => m.Receiver.Equals(receiver));
        if (waitingMessage == null)
          return null;

        waitingPrivateMessages.Remove(waitingMessage);

        return waitingMessage;
      }
    }

    /// <summary>
    /// Асинхронно послыает запрос для регистрации на сервере.
    /// </summary>
    [SecuritySafeCritical]
    public void Register()
    {
      using (var client = ClientModel.Get())
      {
        var sendingContent = new ServerRegisterCommand.MessageContent { User = client.User, OpenKey = ClientModel.Client.OpenKey };
        ClientModel.Client.SendMessage(ServerRegisterCommand.CommandId, sendingContent);
      }
    }

    /// <summary>
    /// Асинхронно посылает запрос для отмены регистрации на сервере.
    /// </summary>
    [SecuritySafeCritical]
    public void Unregister()
    {
      ClientModel.Client.SendMessage(ServerUnregisterCommand.CommandId, null);
    }

    /// <summary>
    /// Создает на сервере комнату.
    /// </summary>
    /// <param name="roomName">Название комнаты для создания.</param>
    /// <param name="type">Тип комнаты.</param>
    [SecuritySafeCritical]
    public void CreateRoom(string roomName, RoomType type)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      var sendingContent = new ServerCreateRoomCommand.MessageContent { RoomName = roomName, Type = type };
      ClientModel.Client.SendMessage(ServerCreateRoomCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Удаляет комнату на сервере. Необходимо являться создателем комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    [SecuritySafeCritical]
    public void DeleteRoom(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      var sendingContent = new ServerDeleteRoomCommand.MessageContent { RoomName = roomName };
      ClientModel.Client.SendMessage(ServerDeleteRoomCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Приглашает в комнату пользователей. Необходимо являться создателем комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="users">Перечисление пользователей, которые будут приглашены.</param>
    [SecuritySafeCritical]
    public void InviteUsers(string roomName, IEnumerable<User> users)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (users == null)
        throw new ArgumentNullException("users");

      var sendingContent = new ServerInviteUsersCommand.MessageContent { RoomName = roomName, Users = users as List<User> ?? users.ToList() };
      ClientModel.Client.SendMessage(ServerInviteUsersCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Удаляет пользователей из комнаты. Необходимо являться создателем комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="users">Перечисление пользователей, которые будут удалены из комнаты.</param>
    [SecuritySafeCritical]
    public void KickUsers(string roomName, IEnumerable<User> users)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (users == null)
        throw new ArgumentNullException("users");

      var sendingContent = new ServerKickUsersCommand.MessageContent { RoomName = roomName, Users = users as List<User> ?? users.ToList() };
      ClientModel.Client.SendMessage(ServerKickUsersCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Осуществляет выход из комнаты пользователя.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    [SecuritySafeCritical]
    public void ExitFromRoom(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      var sendingContent = new ServerExitFromRoomCommand.MessageContent { RoomName = roomName };
      ClientModel.Client.SendMessage(ServerExitFromRoomCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Отправляет запрос о необходимости получения списка пользователей комнаты.
    /// </summary>
    /// <param name="roomName">Название комнтаы.</param>
    [SecuritySafeCritical]
    public void RefreshRoom(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      var sendingContent = new ServerRefreshRoomCommand.MessageContent { RoomName = roomName };
      ClientModel.Client.SendMessage(ServerRefreshRoomCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Изменяет администратора комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="newAdmin">Пользователь назначаемый администратором.</param>
    [SecuritySafeCritical]
    public void SetRoomAdmin(string roomName, User newAdmin)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (newAdmin == null)
        throw new ArgumentNullException("newAdmin");

      var sendingContent = new ServerSetRoomAdminCommand.MessageContent { RoomName = roomName, NewAdmin = newAdmin };
      ClientModel.Client.SendMessage(ServerSetRoomAdminCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Добовляет файл на раздачу.
    /// </summary>
    /// <param name="roomName">Название комнаты в которую добавляется файл.</param>
    /// <param name="path">Путь к добовляемому файлу.</param>
    [SecuritySafeCritical]
    public void AddFileToRoom(string roomName, string path)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (string.IsNullOrEmpty(path))
        throw new ArgumentException("fileName");

      var info = new FileInfo(path);
      if (!info.Exists)
        return;

      using (var client = ClientModel.Get())
      {
        var postedFile = client.PostedFiles.FirstOrDefault(posted =>
          posted.File.Owner.Equals(client.User)
          && string.Equals(posted.ReadStream.Name, path)
          && string.Equals(posted.RoomName, roomName));

        // Отправляем на сервер уже созданный файл (нет необходимости создавать новый id)
        if (postedFile != null)
        {
          var oldSendingContent = new ServerAddFileToRoomCommand.MessageContent { RoomName = roomName, File = postedFile.File };
          ClientModel.Client.SendMessage(ServerAddFileToRoomCommand.CommandId, oldSendingContent);
          return;
        }

        // Создаем новый файл
        var id = 0;
        while (client.PostedFiles.Exists(postFile => postFile.File.ID == id))
          id = idCreator.Next(int.MinValue, int.MaxValue);

        var file = new FileDescription(client.User, info.Length, Path.GetFileName(path), id);

        client.PostedFiles.Add(new PostedFile
        {
          File = file,
          RoomName = roomName,
          ReadStream = new FileStream(path, FileMode.Open, FileAccess.Read)
        });

        var newSendingContent = new ServerAddFileToRoomCommand.MessageContent { RoomName = roomName, File = file };
        ClientModel.Client.SendMessage(ServerAddFileToRoomCommand.CommandId, newSendingContent);
      }
    }

    /// <summary>
    /// Удаляет файл с раздачи.
    /// </summary>
    /// <param name="roomName">Название комнаты из которой удаляется файл.</param>
    /// <param name="file">Описание удаляемого файла.</param>
    [SecuritySafeCritical]
    public void RemoveFileFromRoom(string roomName, FileDescription file)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (file == null)
        throw new ArgumentNullException("file");

      using (var client = ClientModel.Get())
      {
        var postedFile = client.PostedFiles.FirstOrDefault(current => current.File.Equals(file));
        if (postedFile == null)
          return;

        client.PostedFiles.Remove(postedFile);
        postedFile.Dispose();
      }

      var sendingContent = new ServerRemoveFileFromRoomCommand.MessageContent { RoomName = roomName, File = file };
      ClientModel.Client.SendMessage(ServerRemoveFileFromRoomCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Загружает файл.
    /// </summary>
    /// <param name="path">Путь для сохранения файла.</param>
    /// <param name="roomName">Название комнаты где находится файл.</param>
    /// <param name="file">Описание файла.</param>
    [SecuritySafeCritical]
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

      using (var client = ClientModel.Get())
      {
        if (client.DownloadingFiles.Exists(dFile => dFile.File.Equals(file)))
          throw new ModelException(ErrorCode.FileAlreadyDownloading, file);

        if (client.User.Equals(file.Owner))
          throw new ArgumentException("Нельзя скачивать свой файл.");

        client.DownloadingFiles.Add(new DownloadingFile { File = file, FullName = path });
      }

      var sendingContent = new ClientReadFilePartCommand.MessageContent
      {
        File = file,
        Length = AsyncClient.DefaultFilePartSize,
        RoomName = roomName,
        StartPartPosition = 0,
      };

      ClientModel.Peer.SendMessage(file.Owner.Nick, ClientReadFilePartCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Останавлиает загрузку файла.
    /// </summary>
    /// <param name="file">Описание файла.</param>
    /// <param name="leaveLoadedPart">Если значение истино недогруженный файл не будет удалятся.</param>
    [SecuritySafeCritical]
    public void CancelDownloading(FileDescription file, bool leaveLoadedPart = true)
    {
      if (file == null)
        throw new ArgumentNullException("file");

      using (var client = ClientModel.Get())
      {
        var downloadingFile = client.DownloadingFiles.FirstOrDefault(current => current.File.Equals(file));
        if (downloadingFile == null)
          return;

        var filePath = downloadingFile.FullName;
        downloadingFile.Dispose();

        client.DownloadingFiles.Remove(downloadingFile);

        if (File.Exists(filePath) && !leaveLoadedPart)
          File.Delete(filePath);
      }
    }

    /// <summary>
    /// Иницирует соединение к другому клиенту. Если уже соединен, то повторно соединение иницированно не будет.
    /// </summary>
    /// <param name="nick">Ник клиента к которму будет инцированно соединение.</param>
    [SecuritySafeCritical]
    public void ConnectToPeer(string nick)
    {
      if (ClientModel.Peer.IsConnected(nick))
        return;

      var sendingContent = new ServerP2PConnectRequestCommand.MessageContent { Nick = nick };
      ClientModel.Client.SendMessage(ServerP2PConnectRequestCommand.CommandId, sendingContent);
    }

    [SecuritySafeCritical]
    public void PingRequest()
    {
      ClientModel.Client.SendMessage(ServerPingRequestCommand.CommandId, null);
    }
  }
}
