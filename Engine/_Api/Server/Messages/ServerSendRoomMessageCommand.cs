using System;
using System.Security;
using Engine.Api.Client.Messages;
using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Server.Messages
{
  [SecurityCritical]
  class ServerSendRoomMessageCommand :
    ServerCommand<ServerSendRoomMessageCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.SendRoomMessage;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (string.IsNullOrEmpty(content.Text))
        throw new ArgumentException("content.Text");

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

        if (content.MessageId != null && !room.IsMessageBelongToUser(args.ConnectionId, content.MessageId.Value))
        {
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.MessageEditAccessDenied));
          return;
        }

        Message message = null;
        if (content.MessageId == null)
          message = room.AddMessage(args.ConnectionId, content.Text);
        else
        {
          message = room.GetMessage(content.MessageId.Value);
          message.Text = content.Text;
        }

        var sendingContent = new ClientOutRoomMessageCommand.MessageContent
        {
          Message = message.ToDto(),
          RoomName = content.RoomName,
        };

        foreach (var user in room.Users)
          ServerModel.Server.SendMessage(user, ClientOutRoomMessageCommand.CommandId, sendingContent);
      }
    }

    [Serializable]
    [BinType("ServerSendRoomMessage")]
    public class MessageContent
    {
      [BinField("r")]
      public string RoomName;

      [BinField("m")]
      public BinNullable<long> MessageId;

      [BinField("t")]
      public string Text;
    }
  }
}
