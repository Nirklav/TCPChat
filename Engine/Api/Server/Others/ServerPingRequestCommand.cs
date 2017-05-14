using System.Security;
using Engine.Api.Client.Others;
using Engine.Model.Server;

namespace Engine.Api.Server.Others
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
