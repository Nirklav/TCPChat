using Engine.Model.Client;
using Engine.Model.Entities;
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
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (content.Message == MessageId.None)
        throw new ArgumentException("message");

      ClientModel.Notifier.ReceiveMessage(new ReceiveMessageEventArgs
      {
        SystemMessage = content.Message,
        SystemMessageFormat = content.FormatParams,
        Type = MessageType.System
      });
    }

    [Serializable]
    public class MessageContent
    {
      private MessageId message;
      private string[] formatParams;

      public MessageId Message
      {
        get { return message; }
        set { message = value; }
      }

      public string[] FormatParams
      {
        get { return formatParams; }
        set { formatParams = value; }
      }
    }
  }
}
