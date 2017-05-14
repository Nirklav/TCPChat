using Engine.Api.Server.Rooms;
using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.Api.Client.Rooms
{
  [Serializable]
  public class ClientRefreshRoomAction : IAction
  {
    private readonly string _roomName;

    /// <summary>
    /// Send refresh room request on server.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    [SecuritySafeCritical]
    public ClientRefreshRoomAction(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      _roomName = roomName;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var sendingContent = new ServerRefreshRoomCommand.MessageContent { RoomName = _roomName };
      ClientModel.Client.SendMessage(ServerRefreshRoomCommand.CommandId, sendingContent);
    }
  }
}
