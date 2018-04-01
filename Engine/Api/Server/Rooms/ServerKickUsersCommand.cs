using System;
using System.Security;
using Engine.Api.Client.Rooms;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Server.Rooms
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
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException(nameof(content.RoomName));

      if (content.Users == null)
        throw new ArgumentNullException(nameof(content.Users));

      if (content.RoomName == ServerChat.MainRoomName)
      {
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
        return;
      }

      using (var server = ServerModel.Get())
      {
        if (!TryGetRoom(server.Chat, content.RoomName, args.ConnectionId, out Room room))
          return;

        if (room.Admin != args.ConnectionId)
        {
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
          return;
        }

        var sendingContent = new ClientRoomClosedCommand.MessageContent { RoomName = room.Name };

        foreach (var userId in content.Users)
        {
          if (!room.IsUserExist(userId))
            continue;

          if (userId == room.Admin)
          {
            ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
            continue;
          }

          room.RemoveUser(userId);
          ServerModel.Server.SendMessage(userId, ClientRoomClosedCommand.CommandId, sendingContent);
        }

        RefreshRoom(server.Chat, room);
      }
    }

    [Serializable]
    [BinType("ServerKickUsers")]
    public class MessageContent
    {
      [BinField("r")]
      public string RoomName;

      [BinField("u")]
      public UserId[] Users;
    }
  }
}
