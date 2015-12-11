using Engine.API.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Linq;
using System.Security;

namespace Engine.API.ServerCommands
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
    public override void Run(MessageContent content, ServerCommandArgs args)
    {
      if (string.IsNullOrEmpty(content.Message))
        throw new ArgumentException("Message");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("RoomName");

      if (!RoomExists(content.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[content.RoomName];

        if (!room.Users.Contains(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, "Вы не можете отправить сообщение, т.к. не входите в состав этой комнаты.");
          return;
        }

        if (content.MessageId != null && !room.IsMessageBelongToUser(args.ConnectionId, content.MessageId.Value))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, "Вы не можете редактировать это сообщение.");
          return;
        }

        Message message = null;
        if (content.MessageId == null)
          message = room.AddMessage(args.ConnectionId, content.Message);
        else
        {
          message = room.GetMessage(content.MessageId.Value);
          message.Text = content.Message;
        }

        var sendingContent = new ClientOutRoomMessageCommand.MessageContent
        {
          Message = message.Text,
          RoomName = content.RoomName,
          Sender = args.ConnectionId,
          MessageId = message.Id
        };

        foreach (string user in room.Users.Where(u => u != null))
          ServerModel.Server.SendMessage(user, ClientOutRoomMessageCommand.CommandId, sendingContent);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private string message;
      private string roomName;
      private long? messageId;

      public string Message
      {
        get { return message; }
        set { message = value; }
      }

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }

      public long? MessageId
      {
        get { return messageId; }
        set { messageId = value; }
      }
    }
  }
}
