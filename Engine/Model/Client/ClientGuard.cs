using Engine.Model.Client.Entities;
using System.Security;

namespace Engine.Model.Client
{
  public class ClientGuard : ModelGuard<ClientModel>
  {
    [SecurityCritical]
    public ClientGuard(ClientModel model) : base(model)
    {

    }

    public static ClientGuard Current
    {
      [SecuritySafeCritical]
      get { return (ClientGuard)_current; }
    }

    public ClientChat Chat
    {
      [SecuritySafeCritical]
      get { return _model.Chat; }
    }
  }
}
