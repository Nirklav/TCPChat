using System.Collections.Generic;
using System.Security;

namespace Engine.Model.Server
{
  public class ServerGuard : ModelGuard<ServerModel>
  {
    #region constructor
    [SecurityCritical]
    public ServerGuard(ServerModel model) : base(model)
    {

    }
    #endregion

    public ServerGuard Current
    {
      [SecuritySafeCritical]
      get { return (ServerGuard)_current; }
    }

    public Dictionary<string, User> Users
    {
      [SecuritySafeCritical]
      get { return _model.Users; }
    }

    public Dictionary<string, Room> Rooms
    {
      [SecuritySafeCritical]
      get { return _model.Rooms; }
    }
  }
}
