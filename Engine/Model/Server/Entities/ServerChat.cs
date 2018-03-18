using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;

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
    [SecuritySafeCritical]
    public UserDto[] GetRoomUserDtos(string roomName)
    {
      var room = GetRoom(roomName);
      return room.Users
        .Select(CreateDto)
        .ToArray();
    }

    [SecuritySafeCritical]
    private UserDto CreateDto(string nick)
    {
      var user = GetUser(nick);
      var certificate = ServerModel.Server.GetCertificate(nick);
      return new UserDto(user, certificate.RawData);
    }
    #endregion
  }
}
