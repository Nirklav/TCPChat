using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.Api.ClientCommands
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
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (content.Message == null)
        throw new ArgumentNullException("message");

      var receiveMessageArgs = new ReceiveMessageEventArgs
      {
        Type = MessageType.Private,
        Time = DateTime.UtcNow,
        Message = content.Message,
        Sender = args.PeerConnectionId,
      };

      ClientModel.Notifier.ReceiveMessage(receiveMessageArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private string _message;

      public string Message
      {
        get { return _message; }
        set { _message = value; }
      }
    }
  }
}
