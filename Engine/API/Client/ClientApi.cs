using Engine.Api.Server;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using Engine.Plugins;
using Engine.Plugins.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;

namespace Engine.Api.Client
{
  /// <summary>
  /// Класс реализующий стандартный API для клиента.
  /// </summary>
  public sealed class ClientApi :
    CrossDomainObject,
    IApi<ClientCommandArgs>
  {
    [SecurityCritical] private readonly Dictionary<long, ICommand<ClientCommandArgs>> _commands;
    [SecurityCritical] private readonly Dictionary<string, int> _interlocutors;
    [SecurityCritical] private long _lastSendedNumber;

    /// <summary>
    /// Создает экземпляр API.
    /// </summary>
    [SecurityCritical]
    public ClientApi()
    {
      _commands = new Dictionary<long, ICommand<ClientCommandArgs>>();
      _interlocutors = new Dictionary<string, int>();

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
      _commands.Add(command.Id, command);
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
        Number = Interlocked.Increment(ref _lastSendedNumber)
      };

      string userNick;
      using (var client = ClientModel.Get())
        userNick = client.User.Nick;

      lock (_interlocutors)
      {
        foreach (var kvp in _interlocutors)
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
      lock (_interlocutors)
      {
        int count;
        _interlocutors.TryGetValue(nick, out count);
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
      lock (_interlocutors)
      {
        int count;
        _interlocutors.TryGetValue(nick, out count);
        _interlocutors[nick] = count + 1;
      }
    }

    /// <summary>
    /// Удаляет собеседника.
    /// </summary>
    /// <param name="nick">Ник собеседника.</param>
    [SecuritySafeCritical]
    public void RemoveInterlocutor(string nick)
    {
      lock (_interlocutors)
      {
        int count;
        _interlocutors.TryGetValue(nick, out count);
        if (count == 0)
          throw new InvalidOperationException("Can't remove interlocutor");

        if (count == 1)
        {
          _interlocutors.Remove(nick);
          return;
        }

        _interlocutors[nick] = count - 1;
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
      if (_commands.TryGetValue(id, out command))
        return command;

      ClientPluginCommand pluginCommand;
      if (ClientModel.Plugins.TryGetCommand(id, out pluginCommand))
        return pluginCommand;

      return ClientEmptyCommand.Empty;
    }

    /// <summary>
    /// Perform the remote action.
    /// </summary>
    /// <param name="action">Action to perform.</param>
    public void Perform(IAction action)
    {
      action.Pefrorm();
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
