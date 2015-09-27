using Engine.API.ClientCommands;
using Engine.Model.Server;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerPingRequestCommand : ICommand<ServerCommandArgs>
  {
    public const ushort CommandId = (ushort)ServerCommand.PingRequest;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ServerCommandArgs args)
    {
      ServerModel.Server.SendMessage(args.ConnectionId, ClientPingResponceCommand.CommandId, null, true);
    }
  }
}
