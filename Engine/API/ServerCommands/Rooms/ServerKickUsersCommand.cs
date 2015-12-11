using Engine.API.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerKickUsersCommand :
    ServerCommand<ServerKickUsersCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.KickUsers;

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
        ServerModel.Api.SendSystemMessage(args.ConnectionId, "Невозможно удалить пользователей из основной комнаты.");
        return;
      }

      if (!RoomExists(content.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        Room room = server.Rooms[content.RoomName];

        if (!room.Admin.Equals(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, "Вы не являетесь администратором комнаты. Операция отменена.");
          return;
        }

        var sendingContent = new ClientRoomClosedCommand.MessageContent { Room = room };

        foreach (var user in content.Users)
        {
          if (!room.Users.Contains(user.Nick))
            continue;

          if (user.Equals(room.Admin))
          {
            ServerModel.Api.SendSystemMessage(args.ConnectionId, "Невозможно удалить из комнаты администратора.");
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
