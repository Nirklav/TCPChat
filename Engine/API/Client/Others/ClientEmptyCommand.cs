using System.Security;

namespace Engine.Api.Client
{
  [SecurityCritical]
  class ClientEmptyCommand : ClientCommand
  {
    public const long CommandId = (long)ClientCommandId.Empty;
    public static readonly ClientEmptyCommand Empty = new ClientEmptyCommand();

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(CommandArgs args)
    {

    }
  }
}
