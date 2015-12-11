using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientEmptyCommand : ICommand<ClientCommandArgs>
  {
    public const long CommandId = (long)ClientCommandId.Empty;
    public static readonly ClientEmptyCommand Empty = new ClientEmptyCommand();

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
