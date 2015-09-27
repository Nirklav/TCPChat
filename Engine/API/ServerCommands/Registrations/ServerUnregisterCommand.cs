using Engine.Model.Server;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerUnregisterCommand : ICommand<ServerCommandArgs>
  {
    public const ushort CommandId = (ushort)ServerCommand.Unregister;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ServerCommandArgs args)
    {
      ServerModel.API.RemoveUser(args.ConnectionId);
    }
  }
}
