using Engine.API.ClientCommands;
using Engine.Helpers;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerGetUserOpenKeyCommand :
    BaseServerCommand,
    ICommand<ServerCommandArgs>
  {
    public const ushort CommandId = (ushort)ServerCommand.GetUserOpenKeyRequest;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
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

      ServerModel.Server.SendMessage(args.ConnectionId, ClientReceiveUserOpenKeyCommand.CommandId, sendingContent);
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
