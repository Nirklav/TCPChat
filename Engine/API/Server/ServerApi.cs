using Engine.Api.Client;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Plugins;
using Engine.Plugins.Server;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;

namespace Engine.Api.Server
{
  public sealed class ServerApi :
    CrossDomainObject,
    IApi<ServerCommandArgs>
  {
    [SecurityCritical] private readonly Dictionary<long, ICommand<ServerCommandArgs>> _commands;

    /// <summary>
    /// Creates instance of ServerApi.
    /// </summary>
    [SecurityCritical]
    public ServerApi()
    {
      _commands = new Dictionary<long, ICommand<ServerCommandArgs>>();

      AddCommand(new ServerRegisterCommand());
      AddCommand(new ServerUnregisterCommand());
      AddCommand(new ServerSendRoomMessageCommand());
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
      _commands.Add(command.Id, command);
    }

    /// <summary>
    /// Name and version of Api.
    /// </summary>
    public string Name
    {
      [SecuritySafeCritical]
      get { return Api.Name; }
    }

    /// <summary>
    /// Get the command by identifier.
    /// </summary>
    /// <param name="id">Command identifier.</param>
    /// <returns>Command.</returns>
    [SecuritySafeCritical]
    public ICommand<ServerCommandArgs> GetCommand(long id)
    {
      ICommand<ServerCommandArgs> command;
      if (_commands.TryGetValue(id, out command))
        return command;

      ServerPluginCommand pluginCommand;
      if (ServerModel.Plugins.TryGetCommand(id, out pluginCommand))
        return pluginCommand;

      return ServerEmptyCommand.Empty;
    }

    /// <summary>
    /// Perform the remote action.
    /// </summary>
    /// <param name="action">Action to perform.</param>
    public void Perform(IAction action)
    {
      action.Perform();
    }

    /// <summary>
    /// Посылает системное сообщение клиенту.
    /// </summary>
    /// <param name="nick">Пользователь получащий сообщение.</param>
    /// <param name="roomName">Имя комнаты, для которой предназначено системное сообщение.</param>
    /// <param name="message">Сообщение.</param>
    [SecuritySafeCritical]
    public void SendSystemMessage(string nick, SystemMessageId message, params string[] formatParams)
    {
      var sendingContent = new ClientOutSystemMessageCommand.MessageContent { Message = message, FormatParams = formatParams };
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
      using (var server = ServerModel.Get())
      {
        foreach (var room in server.Rooms.Values)
        {
          if (!room.Users.Contains(nick))
            continue;

          room.RemoveUser(nick);

          if (room.Admin == nick)
          {
            room.Admin = room.Users.FirstOrDefault();
            if (room.Admin != null)
              ServerModel.Api.SendSystemMessage(room.Admin, SystemMessageId.RoomAdminChanged, room.Name);
          }

          var sendingContent = new ClientRoomRefreshedCommand.MessageContent
          {
            Room = room,
            Users = GetRoomUsers(server, room)
          };

          foreach (var user in room.Users)
            ServerModel.Server.SendMessage(user, ClientRoomRefreshedCommand.CommandId, sendingContent);
        }

        // Removing user from model after all rooms
        server.Users.Remove(nick);
      }

      // Closing the connection after model clearing
      ServerModel.Server.CloseConnection(nick);
      ServerModel.Notifier.Unregistered(new ServerRegistrationEventArgs(nick));
    }
  }
}
