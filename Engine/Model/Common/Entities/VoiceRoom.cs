using System;
using System.Collections.Generic;

namespace Engine.Model.Common.Entities
{
  [Serializable]
  public class VoiceRoom : Room
  {
    /// <summary>
    /// Create new voice room instance.
    /// </summary>
    /// <param name="admin">Admin nick.</param>
    /// <param name="name">Room name.</param>
    public VoiceRoom(string admin, string name)
      : base(admin, name) 
    {

    }

    /// <summary>
    /// Create new voice room instance.
    /// </summary>
    /// <param name="admin">Admin nick.</param>
    /// <param name="name">Room name.</param>
    /// <param name="initialUsers">Initial room users list.</param>
    public VoiceRoom(string admin, string name, IEnumerable<User> initialUsers)
      : base(admin, name, initialUsers) 
    {

    }
  }
}
