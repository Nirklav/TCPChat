using Engine.Api.Client;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.Api.Server
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
        throw new ArgumentException("content.RoomName");

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server.Chat, content.RoomName, args.ConnectionId, out room))
          return;

        if (!room.IsUserExist(args.ConnectionId))
        {
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
          return;
        }

        var roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent
        {
          Room = room.ToDto(args.ConnectionId),
          Users = server.Chat.GetRoomUserDtos(room.Name)
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
