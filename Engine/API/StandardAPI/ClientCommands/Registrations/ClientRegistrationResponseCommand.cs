using Engine.Model.Client;
using System;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientRegistrationResponseCommand :
      BaseCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFromMessage<MessageContent>(args.Message);

      RegistrationEventArgs eventArgs = new RegistrationEventArgs
      {
        Registered = receivedContent.Registered,
        Message = receivedContent.Message
      };

      ClientModel.OnReceiveRegistrationResponse(this, eventArgs);
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
