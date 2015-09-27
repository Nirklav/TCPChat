using Engine.Helpers;
using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientOutRoomMessageCommand :
    ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.OutRoomMessage;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ClientCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.Sender))
        throw new ArgumentException("sender");

      if (string.IsNullOrEmpty(receivedContent.Message))
        throw new ArgumentException("message");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("roomName");

      using (var client = ClientModel.Get())
      {
        var room = client.Rooms[receivedContent.RoomName];
        room.AddMessage(receivedContent.Sender, receivedContent.MessageId, receivedContent.Message);
      }

      var receiveMessageArgs = new ReceiveMessageEventArgs
      {
        Type = MessageType.Common,
        Message = receivedContent.Message,
        Sender = receivedContent.Sender,
        RoomName = receivedContent.RoomName,
        MessageId = receivedContent.MessageId,
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
