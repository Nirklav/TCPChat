using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Api.Client
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
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      var eventArgs = new RegistrationEventArgs(content.Registered, content.Message);
      ClientModel.Notifier.ReceiveRegistrationResponse(eventArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private bool _registered;
      private SystemMessageId _message;

      public bool Registered
      {
        get { return _registered; }
        set { _registered = value; }
      }

      public SystemMessageId Message
      {
        get { return _message; }
        set { _message = value; }
      }
    }
  }
}
