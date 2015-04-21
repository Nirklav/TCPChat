using Engine.Helpers;
using Engine.Model.Server;
using System;

namespace Engine.API.ServerCommands
{
  class ServerP2PConnectRequestCommand :
      BaseServerCommand,
      ICommand<ServerCommandArgs>
  {
    public void Run(ServerCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (receivedContent.Nick == null)
        throw new ArgumentNullException("Info");

      if (!ServerModel.Server.ContainsConnection(receivedContent.Nick))
      {
        ServerModel.API.SendSystemMessage(args.ConnectionId, "Данного пользователя не существует.");
        return;
      }

      ServerModel.Server.P2PService.Introduce(args.ConnectionId, receivedContent.Nick);
    }

    [Serializable]
    public class MessageContent
    {
      string nick;

      public string Nick { get { return nick; } set { nick = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.P2PConnectRequest;
  }
}
