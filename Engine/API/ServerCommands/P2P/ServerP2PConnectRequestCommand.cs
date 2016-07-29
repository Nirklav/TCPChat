using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerP2PConnectRequestCommand :
    ServerCommand<ServerP2PConnectRequestCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.P2PConnectRequest;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ServerCommandArgs args)
    {
      if (content.Nick == null)
        throw new ArgumentNullException("Info");

      if (!ServerModel.Server.ContainsConnection(content.Nick))
      {
        ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.P2PUserNotExist);
        return;
      }

      ServerModel.Server.P2PService.Introduce(args.ConnectionId, content.Nick);
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
