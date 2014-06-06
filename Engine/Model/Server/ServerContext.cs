using Engine.Model.Entities;
using System.Collections.Generic;

namespace Engine.Model.Server
{
  public class ServerContext : ModelContext<ServerModel>
  {
    #region constructor
    public ServerContext(ServerModel model) : base(model)
    {

    }
    #endregion

    #region properties
    public Dictionary<string, User> Users { get { return model.Users; } }
    public Dictionary<string, Room> Rooms { get { return model.Rooms; } }
    #endregion
  }
}
