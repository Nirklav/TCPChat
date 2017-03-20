using Engine.Api.Server.Rooms;
using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.Api.Client.Rooms
{
  [Serializable]
  public class ClientExitFromRoomAction : IAction
  {
    private readonly string _roomName;

    /// <summary>
    /// Exit from room.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    [SecuritySafeCritical]
    public ClientExitFromRoomAction(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      _roomName = roomName;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var sendingContent = new ServerExitFromRoomCommand.MessageContent { RoomName = _roomName };
      ClientModel.Client.SendMessage(ServerExitFromRoomCommand.CommandId, sendingContent);
    }
  }
}
