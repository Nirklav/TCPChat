using System;
using System.Collections.Generic;

namespace Engine.Model.Entities
{
  public class Chat
  {
    private Dictionary<string, Room> _rooms;
    private Dictionary<string, User> _users;

    public Chat()
    {
      _rooms = new Dictionary<string, Room>();
      _users = new Dictionary<string, User>();
    }

    #region Rooms
    public void AddRoom(Room room)
    {
      _rooms.Add(room.Name, room);
    }

    public Room GetRoom(string roomName)
    {
      Room result;
      if (!_rooms.TryGetValue(roomName, out result))
        throw new ArgumentException("Room does not exist");
      return result;
    }

    public IEnumerable<Room> GetRooms()
    {
      return _rooms.Values;
    }

    public Room RemoveRoom(string name)
    {
      Room room;
      _rooms.TryGetValue(name, out room);
      _rooms.Remove(name);
      return room;
    }
    #endregion

    #region Users
    public void AddUser(User user)
    {
      _users.Add(user.Nick, user);
    }

    public User GetUser(string nick)
    {
      User result;
      if (!_users.TryGetValue(nick, out result))
        throw new ArgumentException("User does not exist");
      return result;
    }

    public IEnumerable<User> GetUsers()
    {
      return _users.Values;
    }

    public User RemoveUser(string nick)
    {
      User user;
      _users.TryGetValue(nick, out user);
      _users.Remove(nick);
      return user;
    }
    #endregion
  }
}
