using Engine.Api.Server;
using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.Api.Client.Rooms
{
  [Serializable]
  public class ClientDeleteRoomAction : IAction
  {
    private readonly string _roomName;

    /// <summary>
    /// Remove room on server.
    /// </summary>
    /// <param name="roomName">Removing room name.</param>
    [SecuritySafeCritical]
    public ClientDeleteRoomAction(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      _roomName = roomName;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var sendingContent = new ServerDeleteRoomCommand.MessageContent { RoomName = _roomName };
      ClientModel.Client.SendMessage(ServerDeleteRoomCommand.CommandId, sendingContent);
    }
  }
}
