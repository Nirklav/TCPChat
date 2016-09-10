using Engine.Api.Client;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Linq;
using System.Security;

namespace Engine.Api.Server
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
        ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomCantLeaveMainRoom);
        return;
      }

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server, content.RoomName, args.ConnectionId, out room))
          return;

        if (!room.Users.Contains(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAccessDenied);
          return;
        }

        room.RemoveUser(args.ConnectionId);
        var closeRoomContent = new ClientRoomClosedCommand.MessageContent { Room = room };
        ServerModel.Server.SendMessage(args.ConnectionId, ClientRoomClosedCommand.CommandId, closeRoomContent);

        if (room.Admin == args.ConnectionId)
        {
          room.Admin = room.Users.FirstOrDefault();
          if (room.Admin != null)
            ServerModel.Api.SendSystemMessage(room.Admin, SystemMessageId.RoomAdminChanged, room.Name);
        }

        RefreshRoom(server, room);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private string _roomName;

      public string RoomName
      {
        get { return _roomName; }
        set { _roomName = value; }
      }
    }
  }
}
