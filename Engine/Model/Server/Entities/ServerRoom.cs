using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Collections.Generic;

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

    #region dto
    public virtual RoomDto ToDto()
    {
      return new RoomDto(_name, _admin, _users, _files.Values, _messages, RoomType.Chat, null);
    }
    #endregion
  }
}
