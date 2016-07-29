using Engine.Model.Client;
using Engine.Model.Entities;
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
      if (content.Message == null)
        throw new ArgumentException("message");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("roomName");

      using (var client = ClientModel.Get())
      {
        var room = client.Rooms[content.RoomName];
        room.AddMessage(content.Message);

        var receiveMessageArgs = new ReceiveMessageEventArgs
        {
          Type = MessageType.Common,
          RoomName = content.RoomName,
          MessageId = content.Message.Id,
          Time = content.Message.Time,
          Message = content.Message.Text,
          Sender = content.Message.Owner,
        };

        ClientModel.Notifier.ReceiveMessage(receiveMessageArgs);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private Message message;
      private string roomName;

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }

      public Message Message
      {
        get { return message; }
        set { message = value; }
      }
    }
  }
}
