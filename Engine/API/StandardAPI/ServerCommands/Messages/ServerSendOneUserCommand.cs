using Engine.API.StandardAPI.ClientCommands;
using Engine.Model.Server;
using Engine.Network.Connections;
using System;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerSendOneUserCommand :
      BaseServerCommand,
      IServerAPICommand
  {
    public void Run(ServerCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

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

      ServerModel.Server.SendMessage(receivedContent.Receiver, ClientOutPrivateMessageCommand.Id, sendingContent);
    }

    [Serializable]
    public class MessageContent
    {
      string receiver;
      byte[] key;
      byte[] message;

      public string Receiver { get { return receiver; } set { receiver = value; } }
      public byte[] Key { get { return key; } set { key = value; } }
      public byte[] Message { get { return message; } set { message = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.SendOneUser;
  }
}
