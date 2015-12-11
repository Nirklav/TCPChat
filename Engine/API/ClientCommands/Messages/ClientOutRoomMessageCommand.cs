using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientOutRoomMessageCommand :
    ClientCommand<ClientOutRoomMessageCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.OutRoomMessage;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (string.IsNullOrEmpty(content.Sender))
        throw new ArgumentException("sender");

      if (string.IsNullOrEmpty(content.Message))
        throw new ArgumentException("message");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("roomName");

      using (var client = ClientModel.Get())
      {
        var room = client.Rooms[content.RoomName];
        room.AddMessage(content.Sender, content.MessageId, content.Message);
      }

      var receiveMessageArgs = new ReceiveMessageEventArgs
      {
        Type = MessageType.Common,
        Message = content.Message,
        Sender = content.Sender,
        RoomName = content.RoomName,
        MessageId = content.MessageId,
      };

      ClientModel.Notifier.ReceiveMessage(receiveMessageArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private string sender;
      private string message;
      private string roomName;
      private long messageId;

      public string Sender
      {
        get { return sender; }
        set { sender = value; }
      }

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

      public long MessageId
      {
        get { return messageId; }
        set { messageId = value; }
      }
    }
  }
}
