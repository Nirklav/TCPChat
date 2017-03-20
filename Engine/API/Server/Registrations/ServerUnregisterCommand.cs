using System.Security;
using Engine.Model.Server;

namespace Engine.Api.Server.Registrations
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
