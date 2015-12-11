using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientPingResponceCommand : ICommand<ClientCommandArgs>
  {
    public const long CommandId = (long)ClientCommandId.PingResponce;

    public long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ClientCommandArgs args)
    {

    }
  }
}
