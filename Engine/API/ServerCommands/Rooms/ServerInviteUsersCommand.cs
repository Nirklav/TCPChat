using Engine.Api.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Security;

namespace Engine.Api.ServerCommands
{
  [SecurityCritical]
  class ServerInviteUsersCommand :
    ServerCommand<ServerInviteUsersCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.InvateUsers;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ServerCommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("RoomName");

      if (content.Users == null)
        throw new ArgumentNullException("Users");

      if (string.Equals(content.RoomName, ServerModel.MainRoomName))
      {
        ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomItsMainRoom);
        return;
      }

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server, content.RoomName, args.ConnectionId, out room))
          return;

        if (!room.Admin.Equals(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAccessDenied);
          return;
        }

        var invitedUsers = new HashSet<string>();
        foreach (var userNick in content.Users)
        {
          if (room.ContainsUser(userNick))
            continue;

          room.AddUser(userNick);
          invitedUsers.Add(userNick);
        }

        var users = ServerModel.Api.GetRoomUsers(server, room);

        var roomOpenContent = new ClientRoomOpenedCommand.MessageContent
        {
          Room = room,
          Type = room.Type,
          Users = users
        };

        var roomRefreshContent = new ClientRoomRefreshedCommand.MessageContent
        {
          Room = room,
          Users = users
        };

        foreach (var userId in room.Users)
        {
          if (invitedUsers.Contains(userId))
            ServerModel.Server.SendMessage(userId, ClientRoomOpenedCommand.CommandId, roomOpenContent);
          else
            ServerModel.Server.SendMessage(userId, ClientRoomRefreshedCommand.CommandId, roomRefreshContent);
        }
      }
    }

    [Serializable]
    public class MessageContent
    {
      private string _roomName;
      private List<string> _users;

      public string RoomName
      {
        get { return _roomName; }
        set { _roomName = value; }
      }

      public List<string> Users
      {
        get { return _users; }
        set { _users = value; }
      }
    }
  }
}
