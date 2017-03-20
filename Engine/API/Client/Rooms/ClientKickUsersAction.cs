using Engine.Api.Server.Rooms;
using Engine.Model.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Api.Client.Rooms
{
  [Serializable]
  public class ClientKickUsersAction : IAction
  {
    private readonly string _roomName;
    private readonly string[] _users;

    /// <summary>
    /// Remove users from room.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    /// <param name="users">Users to be removed.</param>
    [SecuritySafeCritical]
    public ClientKickUsersAction(string roomName, IEnumerable<string> users)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (users == null)
        throw new ArgumentNullException("users");

      _roomName = roomName;
      _users = users as string[] ?? users.ToArray();
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var sendingContent = new ServerKickUsersCommand.MessageContent { RoomName = _roomName, Users = _users };
      ClientModel.Client.SendMessage(ServerKickUsersCommand.CommandId, sendingContent);
    }
  }
}
