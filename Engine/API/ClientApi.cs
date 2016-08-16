using Engine.API.ClientCommands;
using Engine.API.ServerCommands;
using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Entities;
using Engine.Network;
using Engine.Plugins;
using Engine.Plugins.Client;
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
  public sealed class ClientApi :
    CrossDomainObject,
    IApi<ClientCommandArgs>
  {
    private readonly Dictionary<long, ICommand<ClientCommandArgs>> commands;
    private readonly Dictionary<string, int> interlocutors;
    private readonly Random idCreator;
    private long lastSendedNumber;

    /// <summary>
    /// Создает экземпляр API.
    /// </summary>
    [SecurityCritical]
    public ClientApi()
    {
      commands = new Dictionary<long, ICommand<ClientCommandArgs>>();
      interlocutors = new Dictionary<string, int>();
      idCreator = new Random(DateTime.Now.Millisecond);

      ClientModel.Recorder.Recorded += OnRecorded;

      AddCommand(new ClientRegistrationResponseCommand());
      AddCommand(new ClientRoomRefreshedCommand());
      AddCommand(new ClientOutPrivateMessageCommand());
      AddCommand(new ClientOutRoomMessageCommand());
      AddCommand(new ClientOutSystemMessageCommand());
      AddCommand(new ClientFilePostedCommand());
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

      string userNick;
      using (var client = ClientModel.Get())
        userNick = client.User.Nick;

      lock (interlocutors)
      {
        foreach (var kvp in interlocutors)
        {
          var nick = kvp.Key;
          var count = kvp.Value;

          if (count <= 0)
            continue;

          if (nick.Equals(userNick))
            continue;

          ClientModel.Peer.SendMessageIfConnected(nick, ClientPlayVoiceCommand.CommandId, content, true);
        }
      }
    }

    /// <summary>
    /// Возвращает флаг описывающий активность собеседника.
    /// </summary>
    /// <param name="nick">Ник собеседника.</param>
    /// <returns>Флаг описывающий активность собеседника.</returns>
    [SecuritySafeCritical]
    public bool IsActiveInterlocutor(string nick)
    {
      lock (interlocutors)
      {
        int count;
        interlocutors.TryGetValue(nick, out count);
        return count > 0;
      }
    }

    /// <summary>
    /// Добавляет собеседника.
    /// </summary>
    /// <param name="nick">Ник собеседника.</param>
    [SecuritySafeCritical]
    public void AddInterlocutor(string nick)
    {
      lock (interlocutors)
      {
        int count;
        interlocutors.TryGetValue(nick, out count);
        interlocutors[nick] = count + 1;
      }
    }

    /// <summary>
    /// Удаляет собеседника.
    /// </summary>
    /// <param name="nick">Ник собеседника.</param>
    [SecuritySafeCritical]
    public void RemoveInterlocutor(string nick)
    {
      lock (interlocutors)
      {
        int count;
        interlocutors.TryGetValue(nick, out count);
        if (count == 0)
          throw new InvalidOperationException("Can't remove interlocutor");

        if (count == 1)
        {
          interlocutors.Remove(nick);
          return;
        }

        interlocutors[nick] = count - 1;
      }
    }

    /// <summary>
    /// Включает звуковую комнату.
    /// </summary>
    /// <param name="name">Название комнаты.</param>
    [SecuritySafeCritical]
    public void EnableVoiceRoom(string name)
    {
      using (var client = ClientModel.Get())
      {
        var room = GetVoiceRoom(client, name);
        if (!room.Enabled)
        {
          room.Enabled = true;
          foreach (var nick in room.Users)
          {
            if (nick == client.User.Nick)
              continue;

            AddInterlocutor(nick);
          }
        }
      }
    }

    /// <summary>
    /// Выключает звуковую комнату.
    /// </summary>
    /// <param name="name">Название комнаты.</param>
    [SecuritySafeCritical]
    public void DisableVoiceRoom(string name)
    {
      using (var client = ClientModel.Get())
      {
        var room = GetVoiceRoom(client, name);
        if (room.Enabled)
        {
          room.Enabled = false;
          foreach (var nick in room.Users)
          {
            if (nick == client.User.Nick)
              continue;

            RemoveInterlocutor(nick);
          }
        }
      }
    }

    [SecurityCritical]
    private VoiceRoom GetVoiceRoom(ClientGuard client, string name)
    {
      Room room;
      if (!client.Rooms.TryGetValue(name, out room))
        throw new ArgumentException("Room does not exist");
      var voiceRoom = room as VoiceRoom;
      if (voiceRoom == null)
        throw new ArgumentException("This room not voice");
      return voiceRoom;
    }

    /// <summary>
    /// Версия и имя данного API.
    /// </summary>
    public string Name
    {
      [SecuritySafeCritical]
      get { return Api.Name; }
    }

    /// <summary>
    /// Извлекает команду.
    /// </summary>
    /// <param name="message">Сообщение, по которому будет определена команда.</param>
    /// <returns>Команда.</returns>
    [SecuritySafeCritical]
    public ICommand<ClientCommandArgs> GetCommand(long id)
    {
      ICommand<ClientCommandArgs> command;
      if (commands.TryGetValue(id, out command))
        return command;

      ClientPluginCommand pluginCommand;
      if (ClientModel.Plugins.TryGetCommand(id, out pluginCommand))
        return pluginCommand;

      return ClientEmptyCommand.Empty;
    }

    /// <summary>
    /// Отправляет сообщение.
    /// </summary>
    /// <param name="messageId">Идентификатор сообщения. (Если указан - сообщение будет отредактированно) </param>
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
    /// Отправляет сообщение конкретному пользователю.
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

      var sendingContent = new ClientOutPrivateMessageCommand.MessageContent { Message = message };
      ClientModel.Peer.SendMessage(receiver, ClientOutPrivateMessageCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Послыает запрос для регистрации на сервере.
    /// </summary>
    [SecuritySafeCritical]
    public void Register()
    {
      using (var client = ClientModel.Get())
      {
        var sendingContent = new ServerRegisterCommand.MessageContent { User = client.User };
        ClientModel.Client.SendMessage(ServerRegisterCommand.CommandId, sendingContent);
      }
    }

    /// <summary>
    /// Посылает запрос для отмены регистрации на сервере.
    /// </summary>
    [SecuritySafeCritical]
    public void Unregister()
    {
      ClientModel.Client.SendMessage(ServerUnregisterCommand.CommandId);
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
    /// Удаляет комнату на сервере. Необходимо быть администратором комнаты.
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
    /// Приглашает в комнату пользователей. Необходимо быть администратором комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="users">Перечисление пользователей, которые будут приглашены.</param>
    [SecuritySafeCritical]
    public void InviteUsers(string roomName, IEnumerable<string> users)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (users == null)
        throw new ArgumentNullException("users");

      var sendingContent = new ServerInviteUsersCommand.MessageContent { RoomName = roomName, Users = users as List<string> ?? users.ToList() };
      ClientModel.Client.SendMessage(ServerInviteUsersCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Удаляет пользователей из комнаты. Необходимо быть администратором комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="users">Пользотватели, которые будут удалены из комнаты.</param>
    [SecuritySafeCritical]
    public void KickUsers(string roomName, IEnumerable<string> users)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (users == null)
        throw new ArgumentNullException("users");

      var sendingContent = new ServerKickUsersCommand.MessageContent { RoomName = roomName, Users = users as List<string> ?? users.ToList() };
      ClientModel.Client.SendMessage(ServerKickUsersCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Осуществляет выход из комнаты.
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
    /// Отправляет запрос обновления комнаты.
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
    /// Изменяет администратора комнаты. Необходимо быть администратором комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="newAdmin">Пользователь назначаемый администратором.</param>
    [SecuritySafeCritical]
    public void SetRoomAdmin(string roomName, string newAdmin)
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
          posted.File.Id.Owner.Equals(client.User)
          && string.Equals(posted.ReadStream.Name, path)
          && string.Equals(posted.RoomName, roomName));

        // If file already exist then send created file on server.
        if (postedFile != null)
        {
          var oldSendingContent = new ServerAddFileToRoomCommand.MessageContent { RoomName = roomName, File = postedFile.File };
          ClientModel.Client.SendMessage(ServerAddFileToRoomCommand.CommandId, oldSendingContent);
          return;
        }

        // Create new file.
        FileId id;
        while (true)
        {
          id = new FileId(idCreator.Next(int.MinValue, int.MaxValue), client.User.Nick);
          if (!client.PostedFiles.Exists(postFile => postFile.File.Id == id))
            break;
        }
        var file = new FileDescription(id, info.Length, Path.GetFileName(path));

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
    /// <param name="fileId">Идентификатор файла.</param>
    [SecuritySafeCritical]
    public void RemoveFileFromRoom(string roomName, FileId fileId)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      using (var client = ClientModel.Get())
      {
        var postedFileIndex = client.PostedFiles.FindIndex(f => f.File.Id == fileId);
        if (postedFileIndex < 0)
          return;

        var postedFile = client.PostedFiles[postedFileIndex];
        client.PostedFiles.RemoveAt(postedFileIndex);
        postedFile.Dispose();
      }

      var sendingContent = new ServerRemoveFileFromRoomCommand.MessageContent { RoomName = roomName, FileId = fileId };
      ClientModel.Client.SendMessage(ServerRemoveFileFromRoomCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Закрывает раздающийся файл на клиенте. (Без удаления на сервере)
    /// </summary>
    /// <param name="client">Контекст клиента.</param>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="fileId">Идентификатор файла.</param>
    [SecuritySafeCritical]
    public void ClosePostedFile(ClientGuard client, string roomName, FileId fileId)
    {
      // Remove file from room
      Room room;
      if (client.Rooms.TryGetValue(roomName, out room))
        room.Files.RemoveAll(f => f.Id == fileId);

      // Remove downloading files
      var closing = new List<DownloadingFile>();
      client.DownloadingFiles.RemoveAll(f =>
      {
        if (f.File.Id == fileId)
        {
          closing.Add(f);
          return true;
        }
        return false;
      });

      foreach (var file in closing)
        file.Dispose();
     
      // Notify
      var downloadEventArgs = new FileDownloadEventArgs
      {
        RoomName = roomName,
        FileId = fileId,
        Progress = 0
      };

      ClientModel.Notifier.PostedFileDeleted(downloadEventArgs);
    }

    /// <summary>
    /// Загружает файл.
    /// </summary>
    /// <param name="path">Путь для сохранения файла.</param>
    /// <param name="roomName">Название комнаты где находится файл.</param>
    /// <param name="fileId">Идентификатор файла.</param>
    [SecuritySafeCritical]
    public void DownloadFile(string path, string roomName, FileId fileId)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (string.IsNullOrEmpty(path))
        throw new ArgumentException("path");

      if (File.Exists(path))
        throw new ArgumentException("path");

      using (var client = ClientModel.Get())
      {
        if (client.DownloadingFiles.Exists(f => f.File.Id == fileId))
          throw new ModelException(ErrorCode.FileAlreadyDownloading, fileId);

        Room room;
        if (!client.Rooms.TryGetValue(roomName, out room))
          throw new ModelException(ErrorCode.RoomNotFound);

        var file = room.Files.Find(f => f.Id == fileId);
        if (file == null)
          throw new ModelException(ErrorCode.FileInRoomNotFound);

        if (client.User.Equals(file.Id.Owner))
          throw new ModelException(ErrorCode.CantDownloadOwnFile);

        client.DownloadingFiles.Add(new DownloadingFile { File = file, FullName = path });

        var sendingContent = new ClientReadFilePartCommand.MessageContent
        {
          File = file,
          Length = AsyncClient.DefaultFilePartSize,
          RoomName = roomName,
          StartPartPosition = 0,
        };

        ClientModel.Peer.SendMessage(file.Id.Owner, ClientReadFilePartCommand.CommandId, sendingContent);
      }
    }

    /// <summary>
    /// Останавлиает загрузку файла.
    /// </summary>
    /// <param name="fileId">Идентификатор файла.</param>
    /// <param name="leaveLoadedPart">Осталять недокачанный файл или нет.</param>
    [SecuritySafeCritical]
    public void CancelDownloading(FileId fileId, bool leaveLoadedPart = true)
    {
      using (var client = ClientModel.Get())
      {
        var file = client.DownloadingFiles.Find(c => c.File.Id == fileId);
        if (file == null)
          return;

        var filePath = file.FullName;
        file.Dispose();

        client.DownloadingFiles.Remove(file);

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
      ClientModel.Client.SendMessage(ServerPingRequestCommand.CommandId);
    }
  }
}
