using Engine.API.ClientCommands;
using Engine.Model.Entities;
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
        ServerModel.Api.SendSystemMessage(args.ConnectionId, MessageId.RoomCantLeaveMainRoom);
        return;
      }

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server, content.RoomName, args.ConnectionId, out room))
          return;

        if (!room.Users.Contains(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, MessageId.RoomAccessDenied);
          return;
        }

        room.RemoveUser(args.ConnectionId);
        var closeRoomContent = new ClientRoomClosedCommand.MessageContent { Room = room };
        ServerModel.Server.SendMessage(args.ConnectionId, ClientRoomClosedCommand.CommandId, closeRoomContent);

        if (room.Admin == args.ConnectionId)
        {
          room.Admin = room.Users.FirstOrDefault();
          if (room.Admin != null)
            ServerModel.Api.SendSystemMessage(room.Admin, MessageId.RoomAdminChanged, room.Name);
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
