using Engine.Model.Server;

namespace Engine.API.ServerCommands
{
  class ServerUnregisterCommand : ICommand<ServerCommandArgs>
  {
    public void Run(ServerCommandArgs args)
    {
      ServerModel.API.RemoveUser(args.ConnectionId);
    }

    public const ushort Id = (ushort)ServerCommand.Unregister;
  }
}
