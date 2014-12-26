using Engine.API.StandardAPI.ClientCommands;
using Engine.API.StandardAPI.ServerCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Engine.API.StandardAPI
{
  /// <summary>
  /// Класс реазиующий стандартное серверное API.
  /// </summary>
  class StandardServerAPI :
    MarshalByRefObject,
    IServerAPI
  {
    /// <summary>
    /// Версия и имя данного API.
    /// </summary>
    public const string API = "StandardAPI v2.3";

    private Dictionary<ushort, ICommand<ServerCommandArgs>> commands;

    /// <summary>
    /// Создает экземпляр API.
    /// </summary>
    /// <param name="host">Сервер которому будет принадлежать данный API.</param>
    public StandardServerAPI()
    {
      commands = new Dictionary<ushort, ICommand<ServerCommandArgs>>();

      commands.Add(ServerRegisterCommand.Id, new ServerRegisterCommand());
      commands.Add(ServerUnregisterCommand.Id, new ServerUnregisterCommand());
      commands.Add(ServerSendRoomMessageCommand.Id, new ServerSendRoomMessageCommand());
      commands.Add(ServerSendPrivateMessageCommand.Id, new ServerSendPrivateMessageCommand());
      commands.Add(ServerGetUserOpenKeyCommand.Id, new ServerGetUserOpenKeyCommand());
      commands.Add(ServerCreateRoomCommand.Id, new ServerCreateRoomCommand());
      commands.Add(ServerDeleteRoomCommand.Id, new ServerDeleteRoomCommand());
      commands.Add(ServerInviteUsersCommand.Id, new ServerInviteUsersCommand());
      commands.Add(ServerKickUsersCommand.Id, new ServerKickUsersCommand());
      commands.Add(ServerExitFromRoomCommand.Id, new ServerExitFromRoomCommand());
      commands.Add(ServerRefreshRoomCommand.Id, new ServerRefreshRoomCommand());
      commands.Add(ServerSetRoomAdminCommand.Id, new ServerSetRoomAdminCommand());
      commands.Add(ServerAddFileToRoomCommand.Id, new ServerAddFileToRoomCommand());
      commands.Add(ServerRemoveFileFromRoomCommand.Id, new ServerRemoveFileFromRoomCommand());
      commands.Add(ServerP2PConnectRequestCommand.Id, new ServerP2PConnectRequestCommand());
      commands.Add(ServerP2PReadyAcceptCommand.Id, new ServerP2PReadyAcceptCommand());
      commands.Add(ServerPingRequestCommand.Id, new ServerPingRequestCommand());
    }

    /// <summary>
    /// Версия и имя данного API.
    /// </summary>
    public string Name
    {
      get { return API; }
    }

    /// <summary>
    /// Извлекает команду.
    /// </summary>
    /// <param name="message">Пришедшее сообщение, по которому будет определена необходимая для извлекания команда.</param>
    /// <returns>Команда для выполнения.</returns>
    public ICommand<ServerCommandArgs> GetCommand(byte[] message)
    {
      ushort id = BitConverter.ToUInt16(message, 0);

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
    /// <param name="container"></param>
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

        ServerModel.Server.SendMessage(requestId, ClientWaitPeerConnectionCommand.Id, content);
      }
    }

    /// <summary>
    /// Посылает системное сообщение клиенту.
    /// </summary>
    /// <param name="nick">Пользователь получащий сообщение.</param>
    /// <param name="message">Сообщение.</param>
    public void SendSystemMessage(string nick, string message)
    {
      var sendingContent = new ClientOutSystemMessageCommand.MessageContent { Message = message };
      ServerModel.Server.SendMessage(nick, ClientOutSystemMessageCommand.Id, sendingContent);
    }

    /// <summary>
    /// Посылает клиенту запрос на подключение к P2PService
    /// </summary>
    /// <param name="nick">Пользователь получащий запрос.</param>
    public void SendP2PConnectRequest(string nick, int servicePort)
    {
      var sendingContent = new ClientConnectToP2PServiceCommand.MessageContent { Port = servicePort };
      ServerModel.Server.SendMessage(nick, ClientConnectToP2PServiceCommand.Id, sendingContent);
    }

    /// <summary>
    /// Удаляет пользователя и закрывает соединение с ним.
    /// </summary>
    /// <param name="nick">Ник пользователя, соединение котрого будет закрыто.</param>
    public void RemoveUser(string nick)
    {
      ServerModel.Server.CloseConnection(nick);

      using (var server = ServerModel.Get())
      {
        foreach (string roomName in server.Rooms.Keys)
        {
          Room room = server.Rooms[roomName];

          if (!room.Users.Contains(nick))
            continue;

          room.Remove(nick);
          server.Users.Remove(nick);

          if (string.Equals(room.Admin, nick))
          {
            room.Admin = room.Users.FirstOrDefault();

            if (room.Admin != null)
            {
              string message = string.Format("Вы назначены администратором комнаты {0}.", room.Name);
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

            ServerModel.Server.SendMessage(user, ClientRoomRefreshedCommand.Id, sendingContent);
          }
        }
      }

      ServerModel.Notifier.OnUnregistered(new ServerRegistrationEventArgs { Nick = nick });
    }
  }
}
