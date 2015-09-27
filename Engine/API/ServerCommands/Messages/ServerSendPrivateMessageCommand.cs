using Engine.API.ClientCommands;
using Engine.Helpers;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerSendPrivateMessageCommand :
    BaseServerCommand,
    ICommand<ServerCommandArgs>
  {
    public const ushort CommandId = (ushort)ServerCommand.SendPrivateMessage;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ServerCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (receivedContent.Key == null)
        throw new ArgumentNullException("Key");

      if (receivedContent.Message == null)
        throw new ArgumentNullException("Message");

      if (string.IsNullOrEmpty(receivedContent.Receiver))
        throw new ArgumentException("Receiver");

      if (!ServerModel.Server.ContainsConnection(receivedContent.Receiver))
      {
        ServerModel.API.SendSystemMessage(receivedContent.Receiver, "Данного пользователя нет в сети.");
        return;
      }

      var sendingContent = new ClientOutPrivateMessageCommand.MessageContent
      {
        Key = receivedContent.Key,
        Message = receivedContent.Message,
        Sender = args.ConnectionId
      };

      ServerModel.Server.SendMessage(receivedContent.Receiver, ClientOutPrivateMessageCommand.CommandId, sendingContent);
    }

    [Serializable]
    public class MessageContent
    {
      private string receiver;
      private byte[] key;
      private byte[] message;

      public string Receiver
      {
        get { return receiver; }
        set { receiver = value; }
      }

      public byte[] Key
      {
        get { return key; }
        set { key = value; }
      }

      public byte[] Message
      {
        get { return message; }
        set { message = value; }
      }
    }
  }
}
