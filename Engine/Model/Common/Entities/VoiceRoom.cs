using Engine.Model.Common.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Model.Common.Entities
{
  [Serializable]
  public class VoiceRoom : Room
  {
    protected readonly Dictionary<string, List<string>> _connectionMap;

    /// <summary>
    /// Create new voice room instance.
    /// </summary>
    /// <param name="admin">Admin nick.</param>
    /// <param name="name">Room name.</param>
    public VoiceRoom(string admin, string name)
      : base(admin, name) 
    {
      _connectionMap = new Dictionary<string, List<string>>();
      _connectionMap.Add(admin, new List<string>());
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
      _connectionMap = new Dictionary<string, List<string>>();

      for (int i = 0; i < _users.Count; i++)
      {
        var connections = new List<string>();
        for (int m = i + 1; m < _users.Count; m++)
          connections.Add(_users[m]);

        _connectionMap.Add(_users[i], connections);
      }
    }

    #region users
    /// <summary>
    /// Add user to room.
    /// </summary>
    /// <param name="nick">User nick.</param>
    public override void AddUser(string nick)
    {
      base.AddUser(nick);

      // Get users without new
      var users = _connectionMap.Keys.ToList();

      _connectionMap.Add(nick, users);
    }

    /// <summary>
    /// Remove user from room, including all his files.
    /// </summary>
    /// <param name="nick">User nick.</param>
    public override void RemoveUser(string nick)
    {
      base.RemoveUser(nick);

      foreach (var kvp in _connectionMap)
        kvp.Value.Remove(nick);
      _connectionMap.Remove(nick);
    }

    /// <summary>
    /// P2P connections map.
    /// Кey - is user who must initiate connections in value list.
    /// </summary>
    public Dictionary<string, List<string>> ConnectionMap
    {
      get { return _connectionMap; }
    }
    #endregion

    #region dto
    public override RoomDto ToDto()
    {
      return new RoomDto(_name, _admin, _users, _files.Values, _messages, RoomType.Voice, _connectionMap);
    }
    #endregion
  }
}
