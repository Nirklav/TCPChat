using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerEmptyCommand : ICommand<ServerCommandArgs>
  {
    public const ushort CommandId = (ushort)ServerCommand.Empty;
    public static readonly ServerEmptyCommand Empty = new ServerEmptyCommand();

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecurityCritical]
    protected ServerEmptyCommand()
    {

    }

    [SecuritySafeCritical]
    public void Run(ServerCommandArgs args)
    {

    }
  }
}
