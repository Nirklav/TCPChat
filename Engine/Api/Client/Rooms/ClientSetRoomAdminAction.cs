using Engine.Api.Server.Rooms;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Api.Client.Rooms
{
  [Serializable]
  public class ClientSetRoomAdminAction : IAction
  {
    private readonly string _roomName;
    private readonly UserId _newAdmin;

    /// <summary>
    /// Change room admin.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    /// <param name="newAdmin">New room admin.</param>
    [SecuritySafeCritical]
    public ClientSetRoomAdminAction(string roomName, UserId newAdmin)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException(nameof(roomName));

      if (newAdmin == UserId.Empty)
        throw new ArgumentException(nameof(newAdmin));

      _roomName = roomName;
      _newAdmin = newAdmin;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var sendingContent = new ServerSetRoomAdminCommand.MessageContent { RoomName = _roomName, NewAdmin = _newAdmin };
      ClientModel.Client.SendMessage(ServerSetRoomAdminCommand.CommandId, sendingContent);
    }
  }
}
