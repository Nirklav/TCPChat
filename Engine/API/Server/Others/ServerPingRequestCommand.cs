using Engine.Api.Client;
using Engine.Model.Server;
using System.Security;

namespace Engine.Api.Server
{
  [SecurityCritical]
  class ServerPingRequestCommand : ServerCommand
  {
    public const long CommandId = (long)ServerCommandId.PingRequest;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(CommandArgs args)
    {
      ServerModel.Server.SendMessage(args.ConnectionId, ClientPingResponseCommand.CommandId, true);
    }
  }
}
