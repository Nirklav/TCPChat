using Engine.API.ClientCommands;
using Engine.Model.Server;

namespace Engine.API.ServerCommands
{
  class ServerPingRequestCommand : ICommand<ServerCommandArgs>
  {
    public void Run(ServerCommandArgs args)
    {
      ServerModel.Server.SendMessage(args.ConnectionId, ClientPingResponceCommand.Id, null, true);
    }

    public const ushort Id = (ushort)ServerCommand.PingRequest;
  }
}
