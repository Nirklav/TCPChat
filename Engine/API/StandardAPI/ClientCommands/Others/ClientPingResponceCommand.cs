namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientPingResponceCommand : IClientCommand
  {
    public void Run(ClientCommandArgs args)
    {

    }

    public const ushort Id = (ushort)ClientCommand.PingResponce;
  }
}
