using System;

namespace Engine
{
  [Serializable]
  public class RoomOpenedEventArgs : EventArgs
  {
    public string RoomName { get; private set; }

    public RoomOpenedEventArgs(string roomName)
    {
      RoomName = roomName;
    }
  }
}
