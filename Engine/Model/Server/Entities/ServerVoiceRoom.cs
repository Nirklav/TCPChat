using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Model.Server.Entities
{
  [Serializable]
  public class ServerVoiceRoom : VoiceRoom
  {
    protected readonly Dictionary<UserId, List<UserId>> _connectionMap;

    /// <summary>
    /// Create the room.
    /// </summary>
    /// <param name="admin">User id which be administrator of the room.</param>
    /// <param name="name">Room name.</param>
    [SecuritySafeCritical]
    public ServerVoiceRoom(UserId admin, string name)
      : base(admin, name)
    {
      _connectionMap = new Dictionary<UserId, List<UserId>>();
      _connectionMap.Add(admin, new List<UserId>());
    }

    /// <summary>
    /// Create the room.
    /// </summary>
    /// <param name="admin">User id which be administrator of the room.</param>
    /// <param name="name">Room name.</param>
    /// <param name="initialUsers">Initial room users list.</param>
    [SecuritySafeCritical]
    public ServerVoiceRoom(UserId admin, string name, IEnumerable<User> initialUsers)
      : base(admin, name, initialUsers)
    {
      _connectionMap = new Dictionary<UserId, List<UserId>>();

      var users = _users.ToList();
      for (int i = 0; i < users.Count; i++)
      {
        var connections = new List<UserId>();
        for (int m = i + 1; m < _users.Count; m++)
          connections.Add(users[m]);

        _connectionMap.Add(users[i], connections);
      }
    }

    #region users
    /// <summary>
    /// Add user to room.
    /// </summary>
    /// <param name="userId">User id.</param>
    [SecuritySafeCritical]
    public override void AddUser(UserId userId)
    {
      base.AddUser(userId);

      // Get users without new
      var users = new List<UserId>(_connectionMap.Keys);

      _connectionMap.Add(userId, users);
    }

    /// <summary>
    /// Remove user from room, including all his files.
    /// </summary>
    /// <param name="userId">User id.</param>
    [SecuritySafeCritical]
    public override void RemoveUser(UserId userId)
    {
      base.RemoveUser(userId);

      foreach (var kvp in _connectionMap)
        kvp.Value.Remove(userId);
      _connectionMap.Remove(userId);
    }
    #endregion

    #region dto
    [SecuritySafeCritical]
    public override RoomDto ToDto(UserId dtoReceiver)
    {
      List<UserId> connectTo;
      if (!_connectionMap.TryGetValue(dtoReceiver, out connectTo))
        throw new ArgumentException("Reciver not found");
      return new RoomDto(_name, _admin, _users, _files.Values, _messages.Values, RoomType.Voice, connectTo);
    }
    #endregion
  }
}
