using Engine.API.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.API.ServerCommands
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
        ServerModel.Api.SendSystemMessage(args.ConnectionId, MessageId.RoomItsMainRoom);
        return;
      }

      if (!RoomExists(content.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[content.RoomName];

        if (!room.Admin.Equals(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, MessageId.RoomAccessDenied);
          return;
        }

        var invitedUsers = new HashSet<string>();
        foreach (var user in content.Users)
        {
          if (room.ContainsUser(user.Nick))
            continue;

          room.AddUser(user.Nick);
          invitedUsers.Add(user.Nick);
        }

        var users = room.Users
          .Select(nick => server.Users[nick])
          .ToList();

        var roomOpenContent = new ClientRoomOpenedCommand.MessageContent
        {
          Room = room,
          Type = room is VoiceRoom ? RoomType.Voice : RoomType.Chat,
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
      private string roomName;
      private List<User> users;

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }

      public List<User> Users
      {
        get { return users; }
        set { users = value; }
      }
    }
  }
}
