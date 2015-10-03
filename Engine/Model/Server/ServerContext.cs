using Engine.Model.Entities;
using System.Collections.Generic;
using System.Security;

namespace Engine.Model.Server
{
  public class ServerContext : ModelContext<ServerModel>
  {
    #region constructor
    [SecurityCritical]
    public ServerContext(ServerModel model) : base(model)
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
