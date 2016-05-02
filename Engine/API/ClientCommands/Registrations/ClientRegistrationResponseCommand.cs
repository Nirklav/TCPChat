using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Security;

namespace Engine.API.ClientCommands
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
      var eventArgs = new RegistrationEventArgs
      {
        Registered = content.Registered,
        Message = content.Message
      };

      ClientModel.Notifier.ReceiveRegistrationResponse(eventArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private bool registered;
      private MessageId message;

      public bool Registered
      {
        get { return registered; }
        set { registered = value; }
      }

      public MessageId Message
      {
        get { return message; }
        set { message = value; }
      }
    }
  }
}
