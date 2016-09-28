using Engine.Model.Client;
using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Security;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client
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
        throw new ArgumentException("content.Message");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("ontent.RoomName");

      using (var client = ClientModel.Get())
      {
        var room = client.Chat.GetRoom(content.RoomName);
        room.AddMessage(new Message(content.Message));

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
    [BinType("ClientOutRoomMessage")]
    public class MessageContent
    {
      [BinField("m")]
      public MessageDto Message;
      
      [BinField("r")]
      public string RoomName;
    }
  }
}
