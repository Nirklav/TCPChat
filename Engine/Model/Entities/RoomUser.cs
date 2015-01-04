using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Model.Entities
{
  /// <summary>
  /// Пользователь в комнате.
  /// </summary>
  [Serializable]
  public class RoomUser
  {
    private HashSet<long> messageIds;

    /// <summary>
    /// Ник.
    /// </summary>
    public string Nick { get; private set; }

    public RoomUser(string nick)
    {
      Nick = nick;
      messageIds = new HashSet<long>();
    }

    public void AddMessageId(long id) { messageIds.Add(id); }
    public bool ContainsMessage(long id) { return messageIds.Contains(id); }
  }
}
