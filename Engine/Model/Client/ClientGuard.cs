using Engine.Model.Client.Entities;
using System.Security;

namespace Engine.Model.Client
{
  public class ClientGuard : Guard<ClientChat>
  {
    [SecurityCritical]
    public ClientGuard(ClientChat chat)
      : base(chat)
    {

    }

    public ClientChat Chat
    {
      get { return _obj; }
    }

    public static ClientChat CurrentChat
    {
      [SecuritySafeCritical]
      get { return ((ClientGuard)_current).Chat; }
    }
  }
}
