using System;
using System.Security;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.Messages
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
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (content.Message == SystemMessageId.None)
        throw new ArgumentException("content.Message");

      ClientModel.Notifier.ReceiveMessage(new ReceiveMessageEventArgs
      {
        SystemMessage = content.Message,
        SystemMessageFormat = content.FormatParams,
        Time = DateTime.UtcNow,
        Type = MessageType.System
      });
    }

    [Serializable]
    [BinType("ClientOutSystemMessage")]
    public class MessageContent
    {
      [BinField("m")]
      public SystemMessageId Message;

      [BinField("p")]
      public string[] FormatParams;
    }
  }
}
