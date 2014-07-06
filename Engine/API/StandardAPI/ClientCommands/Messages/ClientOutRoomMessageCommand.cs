using Engine.Model.Client;
using System;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientOutRoomMessageCommand :
      BaseCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFromMessage<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.Sender))
        throw new ArgumentException("sender");

      if (string.IsNullOrEmpty(receivedContent.Message))
        throw new ArgumentException("message");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("roomName");

      ReceiveMessageEventArgs receiveMessageArgs = new ReceiveMessageEventArgs
      {
        Type = MessageType.Common,
        Message = receivedContent.Message,
        Sender = receivedContent.Sender,
        RoomName = receivedContent.RoomName,
      };

      ClientModel.OnReceiveMessage(this, receiveMessageArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private string sender;
      private string message;
      private string roomName;

      public string Sender { get { return sender; } set { sender = value; } }
      public string Message { get { return message; } set { message = value; } }
      public string RoomName { get { return roomName; } set { roomName = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.OutRoomMessage;
  }
}
