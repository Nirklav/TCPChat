using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientOutSystemMessageCommand :
    ClientCommand<ClientOutSystemMessageCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.OutSystemMessage;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public override void Run(MessageContent content, ClientCommandArgs args)
    {
      if (string.IsNullOrEmpty(content.Message))
        throw new ArgumentException("message");

      ClientModel.Notifier.ReceiveMessage(new ReceiveMessageEventArgs
      {
        Message = content.Message,
        Type = MessageType.System
      });
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
