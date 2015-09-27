using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientEmptyCommand : ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.Empty;
    public static readonly ClientEmptyCommand Empty = new ClientEmptyCommand();

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
