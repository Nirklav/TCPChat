using Engine.API.StandardAPI.ClientCommands;
using Engine.API.StandardAPI.ServerCommands;
using Engine.Containers;
using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Entities;
using Engine.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Engine.API.StandardAPI
{
  /// <summary>
  /// Класс реализующий стандартный API для клиента.
  /// </summary>
  class StandardClientAPI :
    MarshalByRefObject,
    IClientAPI
  {
    private List<WaitingPrivateMessage> waitingPrivateMessages;
    private Dictionary<ushort, ICommand<ClientCommandArgs>> commands;
    private Random idCreator;
    private long lastSendedNumber;

    /// <summary>
    /// Создает экземпляр API.
    /// </summary>
    /// <param name="host">Клиент которому будет принадлежать данный API.</param>
    public StandardClientAPI()
    {
      commands = new Dictionary<ushort, ICommand<ClientCommandArgs>>();
      waitingPrivateMessages = new List<WaitingPrivateMessage>();
      idCreator = new Random(DateTime.Now.Millisecond);

      ClientModel.Recorder.Recorded += OnRecorded;

      commands.Add(ClientRegistrationResponseCommand.Id, new ClientRegistrationResponseCommand());
      commands.Add(ClientRoomRefreshedCommand.Id, new ClientRoomRefreshedCommand());
      commands.Add(ClientOutRoomMessageCommand.Id, new ClientOutRoomMessageCommand());
      commands.Add(ClientOutPrivateMessageCommand.Id, new ClientOutPrivateMessageCommand());
      commands.Add(ClientOutSystemMessageCommand.Id, new ClientOutSystemMessageCommand());
      commands.Add(ClientFilePostedCommand.Id, new ClientFilePostedCommand());
      commands.Add(ClientReceiveUserOpenKeyCommand.Id, new ClientReceiveUserOpenKeyCommand());
      commands.Add(ClientRoomOpenedCommand.Id, new ClientRoomOpenedCommand());
      commands.Add(ClientRoomClosedCommand.Id, new ClientRoomClosedCommand());
      commands.Add(ClientPostedFileDeletedCommand.Id, new ClientPostedFileDeletedCommand());
      commands.Add(ClientReadFilePartCommand.Id, new ClientReadFilePartCommand());
      commands.Add(ClientWriteFilePartCommand.Id, new ClientWriteFilePartCommand());
      commands.Add(ClientPingResponceCommand.Id, new ClientPingResponceCommand());
      commands.Add(ClientConnectToPeerCommand.Id, new ClientConnectToPeerCommand());
      commands.Add(ClientWaitPeerConnectionCommand.Id, new ClientWaitPeerConnectionCommand());
      commands.Add(ClientConnectToP2PServiceCommand.Id, new ClientConnectToP2PServiceCommand());
      commands.Add(ClientPlayVoiceCommand.Id, new ClientPlayVoiceCommand());
    }

    private void OnRecorded(object sender, RecordedEventArgs e)
    {
      if (!ClientModel.IsInited)
        return;

      byte[] data = new byte[e.DataSize];
      Buffer.BlockCopy(e.Data, 0, data, 0, data.Length);

      ClientPlayVoiceCommand.MessageContent content = new ClientPlayVoiceCommand.MessageContent
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
        List<string> receivers = client.Rooms.Values
          .OfType<VoiceRoom>()
          .SelectMany(r => r.Users)
          .Distinct()
          .ToList();

        receivers.Remove(client.User.Nick);

        ClientModel.Peer.SendMessageIfConnected(receivers, ClientPlayVoiceCommand.Id, content, true);
      }
    }

    /// <summary>
    /// Извлекает команду.
    /// </summary>
    /// <param name="message">Пришедшее сообщение, по которому будет определена необходимая для извлекания команда.</param>
    /// <returns>Команда для выполнения.</returns>
    public ICommand<ClientCommandArgs> GetCommand(byte[] message)
    {
      if (message == null)
        throw new ArgumentNullException("message");

      if (message.Length < 2)
        throw new ArgumentException("message.Length < 2");

      ushort id = BitConverter.ToUInt16(message, 0);

      ICommand<ClientCommandArgs> command;
      if (commands.TryGetValue(id, out command))
        return command;

      if (ClientModel.Plugins.TryGetCommand(id, out command))
        return command;

      return ClientEmptyCommand.Empty;
    }

    /// <summary>
    /// Асинхронно отправляет сообщение всем пользователям в комнате. Если клиента нет в комнате, сообщение игнорируется сервером.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="roomName">Название комнаты.</param>
    public void SendMessage(string message, string roomName)
    {
      if (message == null)
        throw new ArgumentNullException("message");

      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      var sendingContent = new ServerSendRoomMessageCommand.MessageContent { Message = message, RoomName = roomName };
      ClientModel.Client.SendMessage(ServerSendRoomMessageCommand.Id, sendingContent);
    }

    /// <summary>
    /// Асинхронно отправляет сообщение конкретному пользователю.
    /// </summary>
    /// <param name="receiver">Ник получателя.</param>
    /// <param name="message">Сообщение.</param>
    public void SendPrivateMessage(string receiver, string message)
    {
      if (message == null)
        throw new ArgumentNullException("message");

      if (string.IsNullOrEmpty(receiver))
        throw new ArgumentException("receiver");

      lock (waitingPrivateMessages)
        waitingPrivateMessages.Add(new WaitingPrivateMessage { Receiver = receiver, Message = message });

      var sendingContent = new ServerGetUserOpenKeyCommand.MessageContent { Nick = receiver };
      ClientModel.Client.SendMessage(ServerGetUserOpenKeyCommand.Id, sendingContent);
    }

    /// <summary>
    /// Возвращает ожидающее отправки сообщение.
    /// </summary>
    /// <param name="receiver">Имя получателя.</param>
    /// <returns>Ожидающее отправки сообщение.</returns>
    public WaitingPrivateMessage GetWaitingMessage(string receiver)
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
    public void Register()
    {
      using (var client = ClientModel.Get())
      {
        var sendingContent = new ServerRegisterCommand.MessageContent { User = client.User, OpenKey = ClientModel.Client.OpenKey };
        ClientModel.Client.SendMessage(ServerRegisterCommand.Id, sendingContent);
      }
    }

    /// <summary>
    /// Асинхронно посылает запрос для отмены регистрации на сервере.
    /// </summary>
    public void Unregister()
    {
      ClientModel.Client.SendMessage(ServerUnregisterCommand.Id, null);
    }

    /// <summary>
    /// Создает на сервере комнату.
    /// </summary>
    /// <param name="roomName">Название комнаты для создания.</param>
    /// <param name="type">Тип комнаты.</param>
    public void CreateRoom(string roomName, RoomType type)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      var sendingContent = new ServerCreateRoomCommand.MessageContent { RoomName = roomName, Type = type };
      ClientModel.Client.SendMessage(ServerCreateRoomCommand.Id, sendingContent);
    }

    /// <summary>
    /// Удаляет комнату на сервере. Необходимо являться создателем комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    public void DeleteRoom(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      var sendingContent = new ServerDeleteRoomCommand.MessageContent { RoomName = roomName };
      ClientModel.Client.SendMessage(ServerDeleteRoomCommand.Id, sendingContent);
    }

    /// <summary>
    /// Приглашает в комнату пользователей. Необходимо являться создателем комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="users">Перечисление пользователей, которые будут приглашены.</param>
    public void InviteUsers(string roomName, IEnumerable<User> users)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (users == null)
        throw new ArgumentNullException("users");

      var sendingContent = new ServerInviteUsersCommand.MessageContent { RoomName = roomName, Users = users as List<User> ?? users.ToList() };
      ClientModel.Client.SendMessage(ServerInviteUsersCommand.Id, sendingContent);
    }

    /// <summary>
    /// Удаляет пользователей из комнаты. Необходимо являться создателем комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="users">Перечисление пользователей, которые будут удалены из комнаты.</param>
    public void KickUsers(string roomName, IEnumerable<User> users)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (users == null)
        throw new ArgumentNullException("users");

      var sendingContent = new ServerKickUsersCommand.MessageContent { RoomName = roomName, Users = users as List<User> ?? users.ToList() };
      ClientModel.Client.SendMessage(ServerKickUsersCommand.Id, sendingContent);
    }

    /// <summary>
    /// Осуществляет выход из комнаты пользователя.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    public void ExitFromRoom(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      var sendingContent = new ServerExitFromRoomCommand.MessageContent { RoomName = roomName };
      ClientModel.Client.SendMessage(ServerExitFromRoomCommand.Id, sendingContent);
    }

    /// <summary>
    /// Отправляет запрос о необходимости получения списка пользователей комнаты.
    /// </summary>
    /// <param name="roomName">Название комнтаы.</param>
    public void RefreshRoom(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      var sendingContent = new ServerRefreshRoomCommand.MessageContent { RoomName = roomName };
      ClientModel.Client.SendMessage(ServerRefreshRoomCommand.Id, sendingContent);
    }

    /// <summary>
    /// Изменяет администратора комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="newAdmin">Пользователь назначаемый администратором.</param>
    public void SetRoomAdmin(string roomName, User newAdmin)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (newAdmin == null)
        throw new ArgumentNullException("newAdmin");

      var sendingContent = new ServerSetRoomAdminCommand.MessageContent { RoomName = roomName, NewAdmin = newAdmin };
      ClientModel.Client.SendMessage(ServerSetRoomAdminCommand.Id, sendingContent);
    }

    /// <summary>
    /// Добовляет файл на раздачу.
    /// </summary>
    /// <param name="roomName">Название комнаты в которую добавляется файл.</param>
    /// <param name="path">Путь к добовляемому файлу.</param>
    public void AddFileToRoom(string roomName, string path)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (string.IsNullOrEmpty(path))
        throw new ArgumentException("fileName");

      FileInfo info = new FileInfo(path);

      if (!info.Exists)
        return;

      using (var client = ClientModel.Get())
      {
        PostedFile postedFile;
        postedFile = client.PostedFiles.FirstOrDefault(posted =>
          posted.File.Owner.Equals(client.User)
          && string.Equals(posted.ReadStream.Name, path)
          && string.Equals(posted.RoomName, roomName));

        // Отправляем на сервер уже созданный файл (нет необходимости создавать новый id)
        if (postedFile != null)
        {
          var oldSendingContent = new ServerAddFileToRoomCommand.MessageContent { RoomName = roomName, File = postedFile.File };
          ClientModel.Client.SendMessage(ServerAddFileToRoomCommand.Id, oldSendingContent);
          return;
        }

        // Создаем новый файл
        int id = 0;
        while (client.PostedFiles.Exists(postFile => postFile.File.ID == id))
          id = idCreator.Next(int.MinValue, int.MaxValue);

        FileDescription file = new FileDescription(client.User, info.Length, Path.GetFileName(path), id);

        client.PostedFiles.Add(new PostedFile
        {
          File = file,
          RoomName = roomName,
          ReadStream = new FileStream(path, FileMode.Open, FileAccess.Read)
        });

        var newSendingContent = new ServerAddFileToRoomCommand.MessageContent { RoomName = roomName, File = file };
        ClientModel.Client.SendMessage(ServerAddFileToRoomCommand.Id, newSendingContent);
      }
    }

    /// <summary>
    /// Удаляет файл с раздачи.
    /// </summary>
    /// <param name="roomName">Название комнаты из которой удаляется файл.</param>
    /// <param name="file">Описание удаляемого файла.</param>
    public void RemoveFileFromRoom(string roomName, FileDescription file)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (file == null)
        throw new ArgumentNullException("file");

      using (var client = ClientModel.Get())
      {
        PostedFile postedFile = client.PostedFiles.FirstOrDefault(current => current.File.Equals(file));

        if (postedFile == null)
          return;

        client.PostedFiles.Remove(postedFile);
        postedFile.Dispose();
      }

      var sendingContent = new ServerRemoveFileFromRoomCommand.MessageContent { RoomName = roomName, File = file };
      ClientModel.Client.SendMessage(ServerRemoveFileFromRoomCommand.Id, sendingContent);
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

      ClientModel.Peer.SendMessage(file.Owner.Nick, ClientReadFilePartCommand.Id, sendingContent);
    }

    /// <summary>
    /// Останавлиает загрузку файла.
    /// </summary>
    /// <param name="file">Описание файла.</param>
    /// <param name="leaveLoadedPart">Если значение истино недогруженный файл не будет удалятся.</param>
    public void CancelDownloading(FileDescription file, bool leaveLoadedPart = true)
    {
      if (file == null)
        throw new ArgumentNullException("file");

      using (var client = ClientModel.Get())
      {
        DownloadingFile downloadingFile = client.DownloadingFiles.FirstOrDefault(current => current.File.Equals(file));

        if (downloadingFile == null)
          return;

        string filePath = downloadingFile.FullName;
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
    public void ConnectToPeer(string nick)
    {
      if (ClientModel.Peer.IsConnected(nick))
        return;

      var sendingContent = new ServerP2PConnectRequestCommand.MessageContent { Nick = nick };
      ClientModel.Client.SendMessage(ServerP2PConnectRequestCommand.Id, sendingContent);
    }

    public void PingRequest()
    {
      ClientModel.Client.SendMessage(ServerPingRequestCommand.Id, null);
    }
  }
}
