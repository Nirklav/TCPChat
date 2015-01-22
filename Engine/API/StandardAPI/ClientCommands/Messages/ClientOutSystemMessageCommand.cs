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
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.Message))
        throw new ArgumentException("message");

      ClientModel.Notifier.ReceiveMessage(new ReceiveMessageEventArgs { Message = receivedContent.Message, Type = MessageType.System });
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
