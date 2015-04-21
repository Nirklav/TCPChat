using Engine.Helpers;
using Engine.Model.Client;
using System;

namespace Engine.API.ClientCommands
{
  class ClientRegistrationResponseCommand :
      ICommand<ClientCommandArgs>
  {
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
      bool registered;
      string message;

      public bool Registered { get { return registered; } set { registered = value; } }
      public string Message { get { return message; } set { message = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.RegistrationResponse;
  }
}
