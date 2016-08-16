using Engine.Model.Entities;
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

    #region properties
    public Dictionary<string, User> Users
    {
      [SecuritySafeCritical]
      get { return model.Users; }
    }

    public Dictionary<string, Room> Rooms
    {
      [SecuritySafeCritical]
      get { return model.Rooms; }
    }
    #endregion
  }
}
