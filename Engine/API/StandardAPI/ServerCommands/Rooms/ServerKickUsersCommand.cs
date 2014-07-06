using Engine.API.StandardAPI.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network.Connections;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerKickUsersCommand :
      BaseServerCommand,
      IServerAPICommand
  {
    public void Run(ServerCommandArgs args)
    {
      MessageContent receivedContent = GetContentFromMessage<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("RoomName");

      if (receivedContent.Users == null)
        throw new ArgumentNullException("Users");

      if (string.Equals(receivedContent.RoomName, ServerModel.MainRoomName))
      {
        ServerModel.API.SendSystemMessage(args.ConnectionId, "Невозможно удалить пользователей из основной комнаты.");
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

        var sendingContent = new ClientRoomClosedCommand.MessageContent() { Room = room };

        foreach (User user in receivedContent.Users)
        {
          if (!room.Users.Contains(user.Nick))
            continue;

          if (user.Equals(room.Admin))
          {
            ServerModel.API.SendSystemMessage(args.ConnectionId, "Невозможно удалить из комнаты администратора.");
            continue;
          }

          room.Remove(user.Nick);

          ServerModel.Server.SendMessage(user.Nick, ClientRoomClosedCommand.Id, sendingContent);
        }

        foreach (string user in room.Users)
        {
          var roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent
          {
            Room = room,
            Users = room.Users.Select(nick => server.Users[nick]).ToList()
          };
          ServerModel.Server.SendMessage(user, ClientRoomRefreshedCommand.Id, roomRefreshedContent);
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

    public const ushort Id = (ushort)ServerCommand.KickUsers;
  }
}
