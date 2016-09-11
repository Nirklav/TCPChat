using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Model.Common.Entities
{
  [Serializable]
  public abstract class Chat<TUser, TRoom, TVoiceRoom>
    where TUser : User
    where TRoom : Room
    where TVoiceRoom : VoiceRoom
  {
    protected Dictionary<string, TUser> _users;
    protected Dictionary<string, TRoom> _rooms;
    protected Dictionary<string, TVoiceRoom> _voiceRooms;

    [SecurityCritical]
    public Chat()
    {
      _users = new Dictionary<string, TUser>();
      _rooms = new Dictionary<string, TRoom>();
      _voiceRooms = new Dictionary<string, TVoiceRoom>();
    }

    #region Users
    /// <summary>
    /// Returns true if user exist, otherwise false.
    /// </summary>
    /// <param name="nick">User nick.</param>
    [SecuritySafeCritical]
    public bool IsUserExist(string nick)
    {
      return _users.ContainsKey(nick);
    }

    /// <summary>
    /// Add user to room.
    /// </summary>
    /// <param name="user">User that be added to room.</param>
    [SecuritySafeCritical]
    public void AddUser(TUser user)
    {
      _users.Add(user.Nick, user);
    }

    /// <summary>
    /// Get user from room.
    /// </summary>
    /// <param name="nick">Nick of user that be returns.</param>
    /// <returns>User.</returns>
    [SecuritySafeCritical]
    public TUser GetUser(string nick)
    {
      TUser result;
      if (!_users.TryGetValue(nick, out result))
        throw new ArgumentException("User does not exist");
      return result;
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
    /// <param name="nick">Nick of user that be removed.</param>
    /// <returns>Removed user.</returns>
    [SecuritySafeCritical]
    public TUser RemoveUser(string nick)
    {
      TUser user;
      _users.TryGetValue(nick, out user);
      _users.Remove(nick);
      return user;
    }
    #endregion

    #region Rooms
    /// <summary>
    /// Checks the existence of chat rooms by name.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    /// <returns>Returns true if room exist, otherwise false.</returns>
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
      TRoom textRoom;
      if (!_rooms.TryGetValue(roomName, out textRoom))
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
      TVoiceRoom voiceRoom;
      if (!_voiceRooms.TryGetValue(roomName, out voiceRoom))
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
      TRoom textRoom;
      if (_rooms.TryGetValue(roomName, out textRoom))
        return textRoom;

      TVoiceRoom voiceRoom;
      if (_voiceRooms.TryGetValue(roomName, out voiceRoom))
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
    /// Remove text or voice room by name.
    /// </summary>
    /// <param name="roomName">Room name that be removed.</param>
    /// <returns>Removed room.</returns>
    [SecuritySafeCritical]
    public virtual Room RemoveRoom(string roomName)
    {
      TRoom textRoom;
      if (_rooms.TryGetValue(roomName, out textRoom))
      {
        _rooms.Remove(roomName);
        textRoom.Disable();
        return textRoom;
      }

      TVoiceRoom voiceRoom;
      if (_voiceRooms.TryGetValue(roomName, out voiceRoom))
      {
        _voiceRooms.Remove(roomName);
        voiceRoom.Disable();
        return voiceRoom;
      }

      return null;
    }
    #endregion
  }
}
