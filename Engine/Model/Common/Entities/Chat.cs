using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Model.Common.Entities
{
  [Serializable]
  public abstract class Chat<TUser, TRoom, TVoiceRoom> : IDisposable
    where TUser : User
    where TRoom : Room
    where TVoiceRoom : VoiceRoom
  {
    public const string MainRoomName = "Main room";

    protected Dictionary<UserId, TUser> _users;
    protected Dictionary<string, TRoom> _rooms;
    protected Dictionary<string, TVoiceRoom> _voiceRooms;

    [SecurityCritical]
    public Chat()
    {
      _users = new Dictionary<UserId, TUser>();
      _rooms = new Dictionary<string, TRoom>();
      _voiceRooms = new Dictionary<string, TVoiceRoom>();
    }

    #region users
    /// <summary>
    /// Returns true if nick exist, otherwise false.
    /// </summary>
    /// <param name="userId">User id.</param>
    [SecuritySafeCritical]
    public bool IsNickExist(string nick)
    {
      foreach (var userId in _users.Keys)
        if (string.Equals(userId.Nick, nick, StringComparison.Ordinal))
          return true;
      return false;
    }

    /// <summary>
    /// Returns true if user exist, otherwise false.
    /// </summary>
    /// <param name="userId">User id.</param>
    [SecuritySafeCritical]
    public bool IsUserExist(UserId userId)
    {
      return _users.ContainsKey(userId);
    }

    /// <summary>
    /// Add user to room.
    /// </summary>
    /// <param name="user">User that be added to room.</param>
    [SecuritySafeCritical]
    public void AddUser(TUser user)
    {
      _users.Add(user.Id, user);
    }

    /// <summary>
    /// Get user from room.
    /// </summary>
    /// <param name="userId">User id of user who be returns.</param>
    /// <returns>User.</returns>
    [SecuritySafeCritical]
    public TUser GetUser(UserId userId)
    {
      var user = TryGetUser(userId);
      if (user == null)
        throw new ArgumentException("User does not exist");
      return user;
    }

    /// <summary>
    /// Try get user from room.
    /// </summary>
    /// <param name="userId">User id of user who be returns.></param>
    /// <returns>Returns user if he exist, otherwise null.</returns>
    [SecuritySafeCritical]
    public TUser TryGetUser(UserId userId)
    {
      _users.TryGetValue(userId, out var result);
      return result;
    }

    /// <summary>
    /// Seeks user in chat.
    /// </summary>
    /// <param name="nick">User nick of user who be searched.></param>
    /// <returns>Returns user if he exist, otherwise null.</returns>
    [SecuritySafeCritical]
    public TUser FindUser(string nick)
    {
      foreach (var kvp in _users)
      {
        var userId = kvp.Key;
        var user = kvp.Value;
        if (string.Equals(userId.Nick, nick, StringComparison.Ordinal))
          return user;
      }
      return null;
    }

    /// <summary>
    /// Get all users.
    /// </summary>
    [SecuritySafeCritical]
    public IEnumerable<TUser> GetUsers()
    {
      return _users.Values;
    }

    /// <summary>
    /// Remove user from room.
    /// </summary>
    /// <param name="userId">User id of user that be removed.</param>
    /// <returns>Removed user.</returns>
    [SecuritySafeCritical]
    public TUser RemoveUser(UserId userId)
    {
      _users.TryGetValue(userId, out var user);
      _users.Remove(userId);
      return user;
    }
    #endregion

    #region rooms
    /// <summary>
    /// Checks the existence of chat rooms by name.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    /// <returns>Returns true if room exist, otherwise false.</returns>
    [SecuritySafeCritical]
    public bool IsRoomExist(string roomName)
    {
      return _rooms.ContainsKey(roomName) || _voiceRooms.ContainsKey(roomName);
    }

    /// <summary>
    /// Add text room to chat.
    /// </summary>
    /// <param name="room">Text room.</param>
    [SecuritySafeCritical]
    public void AddRoom(TRoom room)
    {
      if (IsRoomExist(room.Name))
        throw new ArgumentException("Room with this name already exist");

      _rooms.Add(room.Name, room);
    }

    /// <summary>
    /// Add voice room to chat.
    /// </summary>
    /// <param name="voiceRoom">Voice room.</param>
    [SecuritySafeCritical]
    public void AddVoiceRoom(TVoiceRoom voiceRoom)
    {
      if (IsRoomExist(voiceRoom.Name))
        throw new ArgumentException("Room with this name already exist");

      _voiceRooms.Add(voiceRoom.Name, voiceRoom);
    }

    /// <summary>
    /// Get text room from chat.
    /// </summary>
    /// <param name="roomName">Text room name.</param>
    /// <returns>Returns text chat room if it exist, otherwise exception will be thrown.</returns>
    [SecuritySafeCritical]
    public TRoom GetTextRoom(string roomName)
    {
      if (!_rooms.TryGetValue(roomName, out var textRoom))
        throw new ArgumentException(string.Format("Room with {0} name not found.", roomName));
      return textRoom;
    }

    /// <summary>
    /// Get voice room from chat.
    /// </summary>
    /// <param name="roomName">Voice room name.</param>
    /// <returns>Returns voice chat room if it exist, otherwise exception will be thrown.</returns>
    [SecuritySafeCritical]
    public TVoiceRoom GetVoiceRoom(string roomName)
    {
      if (!_voiceRooms.TryGetValue(roomName, out var voiceRoom))
        throw new ArgumentException(string.Format("Room with {0} name not found.", roomName));
      return voiceRoom;
    }

    /// <summary>
    /// Get text or voice room from chat.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    /// <returns>Returns chat room if it exist, otherwise exception will be thrown.</returns>
    [SecuritySafeCritical]
    public Room GetRoom(string roomName)
    {
      var room = TryGetRoom(roomName);
      if (room == null)
        throw new ArgumentException(string.Format("Room with {0} name not found.", roomName));
      return room;
    }

    /// <summary>
    /// Get text or voice room from chat.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    /// <returns>Returns chat room if it exist, otherwise null.</returns>
    [SecuritySafeCritical]
    public Room TryGetRoom(string roomName)
    {
      if (_rooms.TryGetValue(roomName, out var textRoom))
        return textRoom;

      if (_voiceRooms.TryGetValue(roomName, out var voiceRoom))
        return voiceRoom;

      return null;
    }

    /// <summary>
    /// Get all rooms from chat.
    /// </summary>
    /// <returns>All text and voice rooms.</returns>
    [SecuritySafeCritical]
    public IEnumerable<Room> GetRooms()
    {
      return Enumerable.Concat((IEnumerable<Room>)_rooms.Values, _voiceRooms.Values);
    }

    /// <summary>
    /// Remove text or voice room by name and dispose it.
    /// </summary>
    /// <param name="roomName">Room name that be removed.</param>
    /// <returns>Removed room.</returns>
    [SecuritySafeCritical]
    public Room RemoveRoom(string roomName)
    {
      if (_rooms.TryGetValue(roomName, out var textRoom))
      {
        _rooms.Remove(roomName);
        textRoom.Disable();
        textRoom.Dispose();
        return textRoom;
      }

      if (_voiceRooms.TryGetValue(roomName, out var voiceRoom))
      {
        _voiceRooms.Remove(roomName);
        voiceRoom.Disable();
        voiceRoom.Dispose();
        return voiceRoom;
      }

      return null;
    }
    #endregion

    #region dispose
    [SecuritySafeCritical]
    protected virtual void ReleaseManagedResources()
    {
      foreach (var room in GetRooms())
        room.Dispose();
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      ReleaseManagedResources();
    }
    #endregion
  }
}
