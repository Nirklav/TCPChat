using Engine.API.ClientCommands;
using Engine.Helpers;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerKickUsersCommand :
    BaseServerCommand,
    ICommand<ServerCommandArgs>
  {
    public const ushort CommandId = (ushort)ServerCommand.KickUsers;

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

        var sendingContent = new ClientRoomClosedCommand.MessageContent { Room = room };

        foreach (var user in receivedContent.Users)
        {
          if (!room.Users.Contains(user.Nick))
            continue;

          if (user.Equals(room.Admin))
          {
            ServerModel.API.SendSystemMessage(args.ConnectionId, "Невозможно удалить из комнаты администратора.");
            continue;
          }

          room.RemoveUser(user.Nick);

          ServerModel.Server.SendMessage(user.Nick, ClientRoomClosedCommand.CommandId, sendingContent);
        }

        RefreshRoom(server, room);
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
