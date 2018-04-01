using System;
using System.Security;
using Engine.Model.Client;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.Messages
{
  [SecurityCritical]
  class ClientOutPrivateMessageCommand
    : ClientCommand<ClientOutPrivateMessageCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.OutPrivateMessage;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    protected override bool IsPeerCommand
    {
      [SecuritySafeCritical]
      get { return true; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (content.Text == null)
        throw new ArgumentNullException(nameof(content.Text));

      var receiveMessageArgs = new ReceiveMessageEventArgs
      {
        Type = MessageType.Private,
        Time = DateTime.UtcNow,
        Message = content.Text,
        Sender = args.ConnectionId,
      };

      ClientModel.Notifier.ReceiveMessage(receiveMessageArgs);
    }

    [Serializable]
    [BinType("ClientOutPrivateMessage")]
    public class MessageContent
    {
      [BinField("t")]
      public string Text;
    }
  }
}
