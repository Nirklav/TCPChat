using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.API.ClientCommands
{
  class ClientOutPrivateMessageCommand
    : ClientCommand<ClientOutPrivateMessageCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.OutPrivateMessage;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public override void Run(MessageContent content, ClientCommandArgs args)
    {
      if (args.PeerConnectionId == null)
        return;

      if (content.Message == null)
        throw new ArgumentNullException("message");

      var receiveMessageArgs = new ReceiveMessageEventArgs
      {
        Type = MessageType.Private,
        Message = content.Message,
        Sender = args.PeerConnectionId,
      };

      ClientModel.Notifier.ReceiveMessage(receiveMessageArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private string message;

      public string Message
      {
        get { return message; }
        set { message = value; }
      }
    }
  }
}
