using Engine.API.ClientCommands;
using Engine.Helpers;
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
    BaseServerCommand,
    ICommand<ServerCommandArgs>
  {
    public const ushort CommandId = (ushort)ServerCommand.InvateUsers;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ServerCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("RoomName");

      if (receivedContent.Users == null)
        throw new ArgumentNullException("Users");

      if (string.Equals(receivedContent.RoomName, ServerModel.MainRoomName))
      {
        ServerModel.API.SendSystemMessage(args.ConnectionId, "Невозможно пригласить пользователей в основную комнату. Они и так все здесь.");
        return;
      }

      if (!RoomExists(receivedContent.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[receivedContent.RoomName];

        if (!room.Admin.Equals(args.ConnectionId))
        {
          ServerModel.API.SendSystemMessage(args.ConnectionId, "Вы не являетесь администратором комнаты. Операция отменена.");
          return;
        }

        var invitedUsers = new List<User>();
        foreach (var user in receivedContent.Users)
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
