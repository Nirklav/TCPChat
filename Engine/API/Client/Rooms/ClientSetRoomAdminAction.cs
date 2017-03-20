using Engine.Api.Server.Rooms;
using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.Api.Client.Rooms
{
  [Serializable]
  public class ClientSetRoomAdminAction : IAction
  {
    private readonly string _roomName;
    private readonly string _newAdmin;

    /// <summary>
    /// Change room admin.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    /// <param name="newAdmin">New room admin.</param>
    [SecuritySafeCritical]
    public ClientSetRoomAdminAction(string roomName, string newAdmin)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (string.IsNullOrEmpty(newAdmin))
        throw new ArgumentException("newAdmin");

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
