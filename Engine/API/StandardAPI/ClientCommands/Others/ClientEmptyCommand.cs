namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientEmptyCommand : ICommand<ClientCommandArgs>
  {
    public void Run(ClientCommandArgs args)
    {

    }

    public static readonly ClientEmptyCommand Empty = new ClientEmptyCommand();

    public const ushort Id = (ushort)ClientCommand.Empty;
  }
}
