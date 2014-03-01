using Engine.Concrete.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Concrete
{
  public class RoomEventArgs : EventArgs
  {
    public Room Room { get; set; }
  }
}
