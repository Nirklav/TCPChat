using System;
using System.Collections.Generic;

namespace Engine
{
  [Serializable]
  public class RoomRefreshedEventArgs : EventArgs
  {
    public string RoomName { get; private set; }
    public HashSet<long> AddedMessages { get; private set; }
    public HashSet<long> RemovedMessages { get; private set; }

    public RoomRefreshedEventArgs(string roomName, HashSet<long> addedMessages, HashSet<long> removedMessages)
    {
      RoomName = roomName;
      AddedMessages = addedMessages;
      RemovedMessages = removedMessages;
    }
  }
}
