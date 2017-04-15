using Engine.Model.Server.Entities;
using System.Security;

namespace Engine.Model.Server
{
  public class ServerGuard : Guard<ServerChat>
  {
    [SecurityCritical]
    public ServerGuard(ServerChat chat)
      : base(chat)
    {

    }

    public ServerChat Chat
    {
      [SecuritySafeCritical]
      get { return _obj; }
    }

    public static ServerChat Current
    {
      [SecuritySafeCritical]
      get { return ((ServerGuard)_current).Chat; }
    }
  }
}
