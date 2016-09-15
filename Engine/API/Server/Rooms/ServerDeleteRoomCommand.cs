using Engine.Api.Client;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using System;
using System.Security;

namespace Engine.Api.Server
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
        throw new ArgumentException("content.RoomName");

      if (content.RoomName == ServerChat.MainRoomName)
      {
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
        return;
      }

      using (var server = ServerModel.Get())
      {
        Room deletingRoom;
        if (!TryGetRoom(server.Chat, content.RoomName, args.ConnectionId, out deletingRoom))
          return;

        if (deletingRoom.Admin != args.ConnectionId)
        {
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
          return;
        }

        server.Chat.RemoveRoom(content.RoomName);

        var sendingContent = new ClientRoomClosedCommand.MessageContent { RoomName = deletingRoom.Name };
        foreach (var user in deletingRoom.Users)
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
