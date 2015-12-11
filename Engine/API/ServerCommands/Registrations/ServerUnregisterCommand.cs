using Engine.Model.Server;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerUnregisterCommand : ICommand<ServerCommandArgs>
  {
    public const long CommandId = (long)ServerCommandId.Unregister;

    public long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ServerCommandArgs args)
    {
      ServerModel.Api.RemoveUser(args.ConnectionId);
    }
  }
}
