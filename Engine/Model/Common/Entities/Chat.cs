using System;
using System.Collections.Generic;
using System.Security;

namespace Engine.Model.Common.Entities
{
  [Serializable]
  public class Chat<TUser, TRoom>
    where TUser : User
    where TRoom : Room
  {
    protected Dictionary<string, TUser> _users;
    protected Dictionary<string, TRoom> _rooms;

    [SecurityCritical]
    public Chat()
    {
      _users = new Dictionary<string, TUser>();
      _rooms = new Dictionary<string, TRoom>();
    }

    #region Users
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
    // TODO: add comments
    [SecuritySafeCritical]
    public void AddRoom(TRoom room)
    {
      _rooms.Add(room.Name, room);
    }

    [SecuritySafeCritical]
    public TRoom GetRoom(string roomName)
    {
      var room = TryGetRoom(roomName);
      if (room != null)
        throw new ArgumentException("Room does not exist");
      return room;
    }

    [SecuritySafeCritical]
    public TRoom TryGetRoom(string roomName)
    {
      TRoom result;
      _rooms.TryGetValue(roomName, out result);
      return result;
    }

    [SecuritySafeCritical]
    public IEnumerable<TRoom> GetRooms()
    {
      return _rooms.Values;
    }

    [SecuritySafeCritical]
    public TRoom RemoveRoom(string name)
    {
      TRoom room;
      _rooms.TryGetValue(name, out room);
      _rooms.Remove(name);
      return room;
    }
    #endregion
  }
}
