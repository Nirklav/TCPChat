namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientPingResponceCommand : IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {

    }

    public const ushort Id = (ushort)ClientCommand.PingResponce;
  }
}
