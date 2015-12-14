using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientPingResponceCommand : ClientCommand
  {
    public const long CommandId = (long)ClientCommandId.PingResponce;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(ClientCommandArgs args)
    {

    }
  }
}
