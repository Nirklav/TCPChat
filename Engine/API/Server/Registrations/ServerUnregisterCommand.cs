using Engine.Api.Server.Registrations;
using Engine.Model.Server;
using System.Security;

namespace Engine.Api.Server
{
  [SecurityCritical]
  class ServerUnregisterCommand : ServerCommand
  {
    public const long CommandId = (long)ServerCommandId.Unregister;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(CommandArgs args)
    {
      ServerModel.Api.Perform(new ServerRemoveUserAction(args.ConnectionId));
    }
  }
}
