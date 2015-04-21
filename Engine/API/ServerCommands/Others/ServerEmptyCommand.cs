namespace Engine.API.ServerCommands
{
  class ServerEmptyCommand : ICommand<ServerCommandArgs>
  {
    protected ServerEmptyCommand()
    {

    }

    public void Run(ServerCommandArgs args)
    {

    }

    public static readonly ServerEmptyCommand Empty = new ServerEmptyCommand();

    public const ushort Id = (ushort)ServerCommand.Empty;
  }
}
