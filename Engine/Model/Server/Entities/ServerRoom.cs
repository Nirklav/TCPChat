using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Model.Server.Entities
{
  [Serializable]
  public class ServerRoom : Room
  {
    /// <summary>
    /// Create the room.
    /// </summary>
    /// <param name="admin">User nick which be administrator of the room.</param>
    /// <param name="name">Room name.</param>
    public ServerRoom(string admin, string name)
      : base(admin, name)
    {

    }

    /// <summary>
    /// Create the room.
    /// </summary>
    /// <param name="admin">User nick which be administrator of the room.</param>
    /// <param name="name">Room name.</param>
    /// <param name="initialUsers">Initial room users list.</param>
    public ServerRoom(string admin, string name, IEnumerable<User> initialUsers)
      : base(admin, name, initialUsers)
    {

    }

    #region users
    public IEnumerable<User> GetUsers()
    {
      return _users.Select(n => ServerGuard.Current.GetUser(n));
    }
    #endregion
  }
}
