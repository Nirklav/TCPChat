using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Api.Client
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
      if (content.Message == SystemMessageId.None)
        throw new ArgumentException("message");

      ClientModel.Notifier.ReceiveMessage(new ReceiveMessageEventArgs
      {
        SystemMessage = content.Message,
        SystemMessageFormat = content.FormatParams,
        Time = DateTime.UtcNow,
        Type = MessageType.System
      });
    }

    [Serializable]
    public class MessageContent
    {
      private SystemMessageId _message;
      private string[] _formatParams;

      public SystemMessageId Message
      {
        get { return _message; }
        set { _message = value; }
      }

      public string[] FormatParams
      {
        get { return _formatParams; }
        set { _formatParams = value; }
      }
    }
  }
}
