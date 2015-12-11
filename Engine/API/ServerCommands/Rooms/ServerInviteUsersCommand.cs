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
        ServerModel.Api.SendSystemMessage(args.ConnectionId, "Невозможно пригласить пользователей в основную комнату. Они и так все здесь.");
        return;
      }

      if (!RoomExists(content.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[content.RoomName];

        if (!room.Admin.Equals(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, "Вы не являетесь администратором комнаты. Операция отменена.");
          return;
        }

        var invitedUsers = new List<User>();
        foreach (var user in content.Users)
        {
          if (room.Users.Contains(user.Nick))
            continue;

          room.AddUser(user.Nick);
          invitedUsers.Add(user);
        }

        var users = room.Users.Select(nick => server.Users[nick]).ToList();
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

        foreach (var user in room.Users)
        {
          if (invitedUsers.Contains(server.Users[user]))
            ServerModel.Server.SendMessage(user, ClientRoomOpenedCommand.CommandId, roomOpenContent);
          else
            ServerModel.Server.SendMessage(user, ClientRoomRefreshedCommand.CommandId, roomRefreshContent);
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
