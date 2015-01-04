using Engine.API.StandardAPI.ClientCommands;
using Engine.Helpers;
using Engine.Model.Server;
using System;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerGetUserOpenKeyCommand :
      BaseServerCommand,
      ICommand<ServerCommandArgs>
  {
    public void Run(ServerCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

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

    public const ushort Id = (ushort)ServerCommand.GetUserOpenKeyRequest;
  }
}
