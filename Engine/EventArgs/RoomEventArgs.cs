using Engine.Model.Entities;
using System;
using System.Collections.Generic;

namespace Engine
{
  [Serializable]
  public class RoomEventArgs : EventArgs
  {
    public Room Room { get; set; }
    public List<User> Users { get; set; }
  }
}
