using Engine.Helpers;
using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientOutSystemMessageCommand :
    ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.OutSystemMessage;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
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
      private string message;

      public string Message
      {
        get { return message; }
        set { message = value; }
      }
    }
  }
}
