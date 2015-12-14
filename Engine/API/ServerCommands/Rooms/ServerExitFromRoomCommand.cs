using Engine.API.ClientCommands;
using Engine.Model.Server;
using System;
using System.Linq;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerExitFromRoomCommand :
    ServerCommand<ServerExitFromRoomCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.ExitFromRoom;

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

      if (string.Equals(content.RoomName, ServerModel.MainRoomName))
      {
        ServerModel.Api.SendSystemMessage(args.ConnectionId, "Невозможно выйти из основной комнаты.");
        return;
      }

      if (!RoomExists(content.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[content.RoomName];

        if (!room.Users.Contains(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, "Вы и так не входите в состав этой комнаты.");
          return;
        }

        room.RemoveUser(args.ConnectionId);
        var closeRoomContent = new ClientRoomClosedCommand.MessageContent { Room = room };
        ServerModel.Server.SendMessage(args.ConnectionId, ClientRoomClosedCommand.CommandId, closeRoomContent);

        if (string.Equals(room.Admin, args.ConnectionId))
        {
          room.Admin = room.Users.FirstOrDefault();

          if (room.Admin != null)
          {
            var message = string.Format("Вы назначены администратором комнаты \"{0}\".", room.Name);
            ServerModel.Api.SendSystemMessage(room.Admin, message);
          }
        }

        RefreshRoom(server, room);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private string roomName;

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }
    }
  }
}
