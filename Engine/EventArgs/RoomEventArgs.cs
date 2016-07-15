using System;
using System.Collections.Generic;

namespace Engine
{
  [Serializable]
  public class RoomEventArgs : EventArgs
  {
    public string RoomName { get; set; }
    public List<string> Users { get; set; }
  }
}
