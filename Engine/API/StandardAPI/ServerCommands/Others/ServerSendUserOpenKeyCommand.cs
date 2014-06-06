using Engine.API.StandardAPI.ClientCommands;
using Engine.Model.Server;
using Engine.Network.Connections;
using System;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerSendUserOpenKeyCommand :
      BaseServerCommand,
      IServerAPICommand
  {
    public void Run(ServerCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.Nick))
        throw new ArgumentException("Nick");

      if (!ServerModel.Server.ContainsConnection(receivedContent.Nick))
      {
        ServerModel.API.SendSystemMessage(receivedContent.Nick, "Данного пользователя нет в сети.");
        return;
      }

      var sendingContent = new ClientReceiveUserOpenKeyCommand.MessageContent
      {
        Nick = receivedContent.Nick,
        OpenKey = ServerModel.Server.GetOpenKey(receivedContent.Nick)
      };

      ServerModel.Server.SendMessage(args.ConnectionId, ClientReceiveUserOpenKeyCommand.Id, sendingContent);
    }

    [Serializable]
    public class MessageContent
    {
      string nick;

      public string Nick { get { return nick; } set { nick = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.UserOpenKeyRequest;
  }
}
