using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientPingResponceCommand : ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.PingResponce;

    public ushort Id
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
