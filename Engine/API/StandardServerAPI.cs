using Engine.API.ClientCommands;
using Engine.API.ServerCommands;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;

namespace Engine.API
{
  /// <summary>
  /// Класс реазиующий стандартное серверное API.
  /// </summary>
  sealed class StandardServerAPI :
    MarshalByRefObject,
    IServerAPI
  {
    /// <summary>
    /// Версия и имя данного API.
    /// </summary>
    public const string API = "StandardAPI v2.3";

    private readonly Dictionary<ushort, ICommand<ServerCommandArgs>> commands;

    /// <summary>
    /// Создает экземпляр API.
    /// </summary>
    [SecurityCritical]
    public StandardServerAPI()
    {
      commands = new Dictionary<ushort, ICommand<ServerCommandArgs>>();

      AddCommand(new ServerRegisterCommand());
      AddCommand(new ServerUnregisterCommand());
      AddCommand(new ServerSendRoomMessageCommand());
      AddCommand(new ServerSendPrivateMessageCommand());
      AddCommand(new ServerGetUserOpenKeyCommand());
      AddCommand(new ServerCreateRoomCommand());
      AddCommand(new ServerDeleteRoomCommand());
      AddCommand(new ServerInviteUsersCommand());
      AddCommand(new ServerKickUsersCommand());
      AddCommand(new ServerExitFromRoomCommand());
      AddCommand(new ServerRefreshRoomCommand());
      AddCommand(new ServerSetRoomAdminCommand());
      AddCommand(new ServerAddFileToRoomCommand());
      AddCommand(new ServerRemoveFileFromRoomCommand());
      AddCommand(new ServerP2PConnectRequestCommand());
      AddCommand(new ServerP2PReadyAcceptCommand());
      AddCommand(new ServerPingRequestCommand());
    }

    [SecurityCritical]
    private void AddCommand(ICommand<ServerCommandArgs> command)
    {
      commands.Add(command.Id, command);
    }

    /// <summary>
    /// Версия и имя данного API.
    /// </summary>
    public string Name
    {
      [SecuritySafeCritical]
      get { return API; }
    }

    /// <summary>
    /// Извлекает команду.
    /// </summary>
    /// <param name="message">Cообщение, по которому будет определена команда.</param>
    /// <returns>Команда.</returns>
    [SecuritySafeCritical]
    public ICommand<ServerCommandArgs> GetCommand(byte[] message)
    {
      if (message == null)
        throw new ArgumentNullException("message");

      if (message.Length < 2)
        throw new ArgumentException("message.Length < 2");

      var id = BitConverter.ToUInt16(message, 0);

      ICommand<ServerCommandArgs> command;
      if (commands.TryGetValue(id, out command))
        return command;

      if (ServerModel.Plugins.TryGetCommand(id, out command))
        return command;

      return ServerEmptyCommand.Empty;
    }

    /// <summary>
    /// Напрямую соединяет пользователей.
    /// </summary>
    /// <param name="senderId">Пользователь запросивший соединение.</param>
    /// <param name="senderPoint">Адрес пользователя запросившего соединение.</param>
    /// <param name="requestId">Запрвшиваемый пользователь.</param>
    /// <param name="requestPoint">Адрес запрашиваемого пользователя.</param>
    [SecuritySafeCritical]
    public void IntroduceConnections(string senderId, IPEndPoint senderPoint, string requestId, IPEndPoint requestPoint)
    {
      using (var context = ServerModel.Get())
      {
        var content = new ClientWaitPeerConnectionCommand.MessageContent
        {
          RequestPoint = requestPoint,
          SenderPoint = senderPoint,
          RemoteInfo = context.Users[senderId],
        };

        ServerModel.Server.SendMessage(requestId, ClientWaitPeerConnectionCommand.CommandId, content);
      }
    }

    /// <summary>
    /// Посылает системное сообщение клиенту.
    /// </summary>
    /// <param name="nick">Пользователь получащий сообщение.</param>
    /// <param name="message">Сообщение.</param>
    [SecuritySafeCritical]
    public void SendSystemMessage(string nick, string message)
    {
      var sendingContent = new ClientOutSystemMessageCommand.MessageContent { Message = message };
      ServerModel.Server.SendMessage(nick, ClientOutSystemMessageCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Посылает клиенту запрос на подключение к P2PService
    /// </summary>
    /// <param name="nick">Пользователь получащий запрос.</param>
    /// <param name="servicePort">Порт сервиса.</param>
    [SecuritySafeCritical]
    public void SendP2PConnectRequest(string nick, int servicePort)
    {
      var sendingContent = new ClientConnectToP2PServiceCommand.MessageContent { Port = servicePort };
      ServerModel.Server.SendMessage(nick, ClientConnectToP2PServiceCommand.CommandId, sendingContent);
    }

    /// <summary>
    /// Удаляет пользователя и закрывает соединение с ним.
    /// </summary>
    /// <param name="nick">Ник пользователя, соединение котрого будет закрыто.</param>
    [SecuritySafeCritical]
    public void RemoveUser(string nick)
    {
      ServerModel.Server.CloseConnection(nick);

      using (var server = ServerModel.Get())
      {
        foreach (string roomName in server.Rooms.Keys)
        {
          var room = server.Rooms[roomName];
          if (!room.Users.Contains(nick))
            continue;

          room.RemoveUser(nick);
          server.Users.Remove(nick);

          if (string.Equals(room.Admin, nick))
          {
            room.Admin = room.Users.FirstOrDefault();

            if (room.Admin != null)
            {
              var message = string.Format("Вы назначены администратором комнаты {0}.", room.Name);
              ServerModel.API.SendSystemMessage(room.Admin, message);
            }
          }

          var sendingContent = new ClientRoomRefreshedCommand.MessageContent
          {
            Room = room,
            Users = room.Users.Select(n => server.Users[n]).ToList()
          };

          foreach (string user in room.Users)
          {
            if (user == null)
              continue;

            ServerModel.Server.SendMessage(user, ClientRoomRefreshedCommand.CommandId, sendingContent);
          }
        }
      }

      ServerModel.Notifier.Unregistered(new ServerRegistrationEventArgs { Nick = nick });
    }
  }
}
