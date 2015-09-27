using Engine.Helpers;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerP2PConnectRequestCommand :
    BaseServerCommand,
    ICommand<ServerCommandArgs>
  {
    public const ushort CommandId = (ushort)ServerCommand.P2PConnectRequest;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
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
      private string nick;

      public string Nick
      {
        get { return nick; }
        set { nick = value; }
      }
    }
  }
}
