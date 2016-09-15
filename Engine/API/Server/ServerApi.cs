using Engine.Api.Client;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Plugins;
using Engine.Plugins.Server;
using System.Collections.Generic;
using System.Linq;
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
    /// Removes user form chat and close all him resources.
    /// </summary>
    /// <param name="nick">User nick who be removed.</param>
    [SecuritySafeCritical]
    public void RemoveUser(string nick)
    {
      using (var server = ServerModel.Get())
      {
        var emptyRooms = new List<string>();
        foreach (var room in server.Chat.GetRooms())
        {
          if (!room.Users.Contains(nick))
            continue;

          room.RemoveUser(nick);
          if (room.IsEmpty)
            emptyRooms.Add(room.Name);
          else
          {
            if (room.Admin == nick)
            {
              room.Admin = room.Users.FirstOrDefault();
              if (room.Admin != null)
                ServerModel.Api.Perform(new ServerSendSystemMessageAction(room.Admin, SystemMessageId.RoomAdminChanged, room.Name));
            }

            foreach (var userNick in room.Users)
            {
              var sendingContent = new ClientRoomRefreshedCommand.MessageContent
              {
                Room = room.ToDto(userNick),
                Users = server.Chat.GetRoomUserDtos(room.Name)
              };
              ServerModel.Server.SendMessage(userNick, ClientRoomRefreshedCommand.CommandId, sendingContent);
            }
          }

        }

        // Remove all empty rooms
        foreach (var emptyRoomName in emptyRooms)
          server.Chat.RemoveRoom(emptyRoomName);

        // Removing user from chat after all rooms processing
        server.Chat.RemoveUser(nick);
      }

      // Closing the connection after model clearing
      ServerModel.Server.CloseConnection(nick);
      ServerModel.Notifier.Unregistered(new ServerRegistrationEventArgs(nick));
    }
  }
}
