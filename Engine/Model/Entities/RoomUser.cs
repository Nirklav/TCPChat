using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Model.Entities
{
  [Serializable]
  public class RoomUser
  {
    private HashSet<long> messageIds;
    public string Nick { get; private set; }

    public RoomUser(string nick)
    {
      Nick = nick;
      messageIds = new HashSet<long>();
    }

    public void AddId(long id) { messageIds.Add(id); }
    public bool ContainsId(long id) { return messageIds.Contains(id); }
  }
}
