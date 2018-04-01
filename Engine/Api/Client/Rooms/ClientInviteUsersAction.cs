using Engine.Api.Server.Rooms;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Api.Client.Rooms
{
  [Serializable]
  public class ClientInviteUsersAction : IAction
  {
    private readonly string _roomName;
    private readonly UserId[] _users;

    /// <summary>
    /// Invites users to room.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    /// <param name="users">Users to be invited.</param>
    [SecuritySafeCritical]
    public ClientInviteUsersAction(string roomName, IEnumerable<UserId> users)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (users == null)
        throw new ArgumentNullException("users");

      _roomName = roomName;
      _users = users as UserId[] ?? users.ToArray();
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var sendingContent = new ServerInviteUsersCommand.MessageContent { RoomName = _roomName, Users = _users };
      ClientModel.Client.SendMessage(ServerInviteUsersCommand.CommandId, sendingContent);
    }
  }
}
