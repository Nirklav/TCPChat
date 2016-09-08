using Engine.Api.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.Api.ServerCommands
{
  [SecurityCritical]
  class ServerRefreshRoomCommand :
    ServerCommand<ServerRefreshRoomCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.RefreshRoom;

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
        return;

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

        var roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent
        {
          Room = room,
          Users = ServerModel.Api.GetRoomUsers(server, room)
        };
        ServerModel.Server.SendMessage(args.ConnectionId, ClientRoomRefreshedCommand.CommandId, roomRefreshedContent);
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
