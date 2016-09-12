using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Model.Server.Entities
{
  [Serializable]
  public class ServerVoiceRoom : VoiceRoom
  {
    protected readonly Dictionary<string, List<string>> _connectionMap;

    /// <summary>
    /// Create the room.
    /// </summary>
    /// <param name="admin">User nick which be administrator of the room.</param>
    /// <param name="name">Room name.</param>
    public ServerVoiceRoom(string admin, string name)
      : base(admin, name)
    {
      _connectionMap = new Dictionary<string, List<string>>();
      _connectionMap.Add(admin, new List<string>());
    }

    /// <summary>
    /// Create the room.
    /// </summary>
    /// <param name="admin">User nick which be administrator of the room.</param>
    /// <param name="name">Room name.</param>
    /// <param name="initialUsers">Initial room users list.</param>
    public ServerVoiceRoom(string admin, string name, IEnumerable<User> initialUsers)
      : base(admin, name, initialUsers)
    {
      _connectionMap = new Dictionary<string, List<string>>();

      var users = _users.ToList();
      for (int i = 0; i < users.Count; i++)
      {
        var connections = new List<string>();
        for (int m = i + 1; m < _users.Count; m++)
          connections.Add(users[m]);

        _connectionMap.Add(users[i], connections);
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
      var users = new List<string>(_connectionMap.Keys);

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
    #endregion

    #region dto
    public RoomDto ToDto(string receiver)
    {
      List<string> connectTo;
      if (!_connectionMap.TryGetValue(receiver, out connectTo))
        throw new ArgumentException("Reciver not found");
      return new RoomDto(_name, _admin, _users, _files.Values, _messages, RoomType.Voice, connectTo);
    }
    #endregion
  }
}
