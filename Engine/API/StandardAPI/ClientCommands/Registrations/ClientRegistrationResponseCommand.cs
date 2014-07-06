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
      ClientModel.OnReceiveRegistrationResponse(this, new RegistrationEventArgs { Registered = receivedContent.Registered });
    }

    [Serializable]
    public class MessageContent
    {
      bool registered;

      public bool Registered { get { return registered; } set { registered = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.RegistrationResponse;
  }
}
