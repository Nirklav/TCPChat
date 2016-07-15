using System;

namespace Engine
{
  public class RoomUsersEventArgs : EventArgs
  {
    public string Nick { get; private set; }
    public bool Removed { get; private set; }

    public RoomUsersEventArgs(string nick, bool removed)
    {
      Nick = nick;
      Removed = removed;
    }
  }
}
