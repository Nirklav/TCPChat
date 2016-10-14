using System.Security;

namespace Engine.Api.Client
{
  [SecurityCritical]
  class ClientPingResponseCommand : ClientCommand
  {
    public const long CommandId = (long)ClientCommandId.PingResponce;

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
