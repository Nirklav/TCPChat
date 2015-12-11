using Engine.Exceptions;
using System.Security;

namespace Engine.API.ClientCommands
{
  abstract class ClientCommand<TContent>
    : Command<TContent, ClientCommandArgs>
  {
    protected virtual bool IsPeerCommand
    {
      [SecuritySafeCritical]
      get { return false; }
    }

    [SecuritySafeCritical]
    protected sealed override void Run(TContent content, ClientCommandArgs args)
    {
      if (IsPeerCommand)
      {
        if (args.PeerConnectionId == null)
          throw new ModelException(ErrorCode.IllegalInvoker, string.Format("Command cannot be runned from server package. {0}", GetType().FullName));
      }
      else
      {
        if (args.PeerConnectionId != null)
          throw new ModelException(ErrorCode.IllegalInvoker, string.Format("Command cannot be runned from peer package. {0}", GetType().FullName));
      }

      OnRun(content, args);
    }

    [SecuritySafeCritical]
    protected abstract void OnRun(TContent content, ClientCommandArgs args);
  }
}
