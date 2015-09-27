using Engine.Helpers;
using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientRegistrationResponseCommand :
    ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.RegistrationResponse;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ClientCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      var eventArgs = new RegistrationEventArgs
      {
        Registered = receivedContent.Registered,
        Message = receivedContent.Message
      };

      ClientModel.Notifier.ReceiveRegistrationResponse(eventArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private bool registered;
      private string message;

      public bool Registered
      {
        get { return registered; }
        set { registered = value; }
      }

      public string Message
      {
        get { return message; }
        set { message = value; }
      }
    }
  }
}
