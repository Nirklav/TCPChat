using Engine.API.StandardAPI.ClientCommands;
using Engine.Helpers;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerInviteUsersCommand :
      BaseServerCommand,
      IServerCommand
  {
    public void Run(ServerCommandArgs args)
    {
      MessageContent receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

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
        Room room = server.Rooms[receivedContent.RoomName];

        if (!room.Admin.Equals(args.ConnectionId))
        {
          ServerModel.API.SendSystemMessage(args.ConnectionId, "Вы не являетесь администратором комнаты. Операция отменена.");
          return;
        }

        List<User> invitedUsers = new List<User>();
        foreach (User user in receivedContent.Users)
        {
          if (room.Users.Contains(user.Nick))
            continue;

          room.Add(user.Nick);
          invitedUsers.Add(user);
        }

        var sendingContent = new ClientRoomOpenedCommand.MessageContent
        {
          Room = room,
          Type = room is VoiceRoom ? RoomType.Voice : RoomType.Chat,
          Users = room.Users.Select(nick => server.Users[nick]).ToList()
        };

        foreach (string user in room.Users)
        {
          if (invitedUsers.Contains(server.Users[user]))
            ServerModel.Server.SendMessage(user, ClientRoomOpenedCommand.Id, sendingContent);
          else
          {
            var roomRefreshContent = new ClientRoomRefreshedCommand.MessageContent
            {
              Room = room,
              Users = room.Users.Select(nick => server.Users[nick]).ToList()
            };
            ServerModel.Server.SendMessage(user, ClientRoomRefreshedCommand.Id, roomRefreshContent);
          }
        }
      }
    }

    [Serializable]
    public class MessageContent
    {
      string roomName;
      IEnumerable<User> users;

      public string RoomName { get { return roomName; } set { roomName = value; } }
      public IEnumerable<User> Users { get { return users; } set { users = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.InvateUsers;
  }
}
