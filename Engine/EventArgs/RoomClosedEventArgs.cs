using System;

namespace Engine
{
  [Serializable]
  public class RoomClosedEventArgs : EventArgs
  {
    public string RoomName { get; private set; }

    public RoomClosedEventArgs(string roomName)
    {
      RoomName = roomName;
    }
  }
}

