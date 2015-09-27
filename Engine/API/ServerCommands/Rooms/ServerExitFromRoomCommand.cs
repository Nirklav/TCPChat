using Engine.API.ClientCommands;
using Engine.Helpers;
using Engine.Model.Server;
using System;
using System.Linq;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerExitFromRoomCommand :
    BaseServerCommand,
    ICommand<ServerCommandArgs>
  {
    public const ushort CommandId = (ushort)ServerCommand.ExitFromRoom;

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

      if (string.Equals(receivedContent.RoomName, ServerModel.MainRoomName))
      {
        ServerModel.API.SendSystemMessage(args.ConnectionId, "Невозможно выйти из основной комнаты.");
        return;
      }

      if (!RoomExists(receivedContent.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[receivedContent.RoomName];

        if (!room.Users.Contains(args.ConnectionId))
        {
          ServerModel.API.SendSystemMessage(args.ConnectionId, "Вы и так не входите в состав этой комнаты.");
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
            string message = string.Format("Вы назначены администратором комнаты \"{0}\".", room.Name);
            ServerModel.API.SendSystemMessage(room.Admin, message);
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
