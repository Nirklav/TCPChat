using System.Security;

namespace Engine.Api.Server
{
  [SecurityCritical]
  class ServerEmptyCommand : ServerCommand
  {
    public const long CommandId = (long)ServerCommandId.Empty;
    public static readonly ServerEmptyCommand Empty = new ServerEmptyCommand();

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecurityCritical]
    protected ServerEmptyCommand()
    {

    }

    [SecuritySafeCritical]
    protected override void OnRun(CommandArgs args)
    {

    }
  }
}
