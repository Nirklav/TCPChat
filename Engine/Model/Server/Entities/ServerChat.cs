using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Model.Server.Entities
{
  [Serializable]
  public class ServerChat : Chat<User, ServerRoom, ServerVoiceRoom>
  {
    [SecurityCritical]
    public ServerChat()
    {
      AddRoom(new ServerRoom(null, MainRoomName));
    }

    #region users
    /// <summary>
    /// Returns all users from room as dtos.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    public List<UserDto> GetRoomUserDtos(string roomName)
    {
      var room = GetRoom(roomName);
      return room.Users
        .Select(n => new UserDto(GetUser(n)))
        .ToList();
    }
    #endregion
  }
}
