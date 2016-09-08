using Engine.Api.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.Api.ServerCommands
{
  [SecurityCritical]
  class ServerDeleteRoomCommand :
    ServerCommand<ServerDeleteRoomCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.DeleteRoom;

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
        ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAccessDenied);
        return;
      }

      using (var server = ServerModel.Get())
      {
        Room deletingRoom;
        if (!TryGetRoom(server, content.RoomName, args.ConnectionId, out deletingRoom))
          return;

        if (!deletingRoom.Admin.Equals(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAccessDenied);
          return;
        }

        server.Rooms.Remove(deletingRoom.Name);

        var sendingContent = new ClientRoomClosedCommand.MessageContent { Room = deletingRoom };
        foreach (string user in deletingRoom.Users)
          ServerModel.Server.SendMessage(user, ClientRoomClosedCommand.CommandId, sendingContent);
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
