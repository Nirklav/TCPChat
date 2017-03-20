using System;
using System.Security;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Server.P2P
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
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (content.Nick == null)
        throw new ArgumentNullException("content.Nick");

      if (!ServerModel.Server.ContainsConnection(content.Nick))
      {
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.P2PUserNotExist));
        return;
      }

      ServerModel.Server.P2PService.Introduce(args.ConnectionId, content.Nick);
    }

    [Serializable]
    [BinType("ServerP2PConnectRequest")]
    public class MessageContent
    {
      [BinField("n")]
      public string Nick;
    }
  }
}
