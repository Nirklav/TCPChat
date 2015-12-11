using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerEmptyCommand : ICommand<ServerCommandArgs>
  {
    public const long CommandId = (long)ServerCommandId.Empty;
    public static readonly ServerEmptyCommand Empty = new ServerEmptyCommand();

    public long Id
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
