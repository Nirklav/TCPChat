namespace Engine.API.ClientCommands
{
  class ClientPingResponceCommand : ICommand<ClientCommandArgs>
  {
    public void Run(ClientCommandArgs args)
    {

    }

    public const ushort Id = (ushort)ClientCommand.PingResponce;
  }
}
