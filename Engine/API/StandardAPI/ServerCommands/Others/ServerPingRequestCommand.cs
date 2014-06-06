using Engine.API.StandardAPI.ClientCommands;
using Engine.Model.Server;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerPingRequestCommand : IServerAPICommand
  {
    public void Run(ServerCommandArgs args)
    {
      ServerModel.Server.SendMessage(args.ConnectionId, ClientPingResponceCommand.Id, null);
    }

    public const ushort Id = (ushort)ServerCommand.PingRequest;
  }
}
