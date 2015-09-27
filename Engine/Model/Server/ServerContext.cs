using Engine.Model.Entities;
using System.Collections.Generic;
using System.Security;

namespace Engine.Model.Server
{
  [SecurityCritical]
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
      [SecurityCritical]
      get { return model.Users; }
    }

    public Dictionary<string, Room> Rooms
    {
      [SecurityCritical]
      get { return model.Rooms; }
    }
    #endregion
  }
}
