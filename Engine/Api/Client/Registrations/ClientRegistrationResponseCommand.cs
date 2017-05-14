using System;
using System.Security;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.Registrations
{
  [SecurityCritical]
  class ClientRegistrationResponseCommand :
    ClientCommand<ClientRegistrationResponseCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.RegistrationResponse;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      var eventArgs = new RegistrationEventArgs(content.Registered, content.Message);
      ClientModel.Notifier.ReceiveRegistrationResponse(eventArgs);
    }

    [Serializable]
    [BinType("ClientRegistrationResponse")]
    public class MessageContent
    {
      [BinField("r")]
      public bool Registered;

      [BinField("m")]
      public SystemMessageId Message;
    }
  }
}
