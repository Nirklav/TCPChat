using Engine.Helpers;
using Engine.Model.Client;
using System;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientOutSystemMessageCommand :
      ICommand<ClientCommandArgs>
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.Message))
        throw new ArgumentException("message");

      ClientModel.Notifier.SystemMessage(receivedContent.Message);
    }

    [Serializable]
    public class MessageContent
    {
      string message;

      public string Message { get { return message; } set { message = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.OutSystemMessage;
  }
}
