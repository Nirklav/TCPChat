using Engine.Api.Server;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Api.Client.Rooms
{
  [Serializable]
  public class ClientCreateRoomAction : IAction
  {
    private readonly string _roomName;
    private readonly RoomType _type;

    /// <summary>
    /// Creates room on server.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    /// <param name="type">Room type.</param>
    [SecuritySafeCritical]
    public ClientCreateRoomAction(string roomName, RoomType type)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (type == RoomType.Unknown)
        throw new ArgumentException("type");

      _roomName = roomName;
      _type = type;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var sendingContent = new ServerCreateRoomCommand.MessageContent { RoomName = _roomName, Type = _type };
      ClientModel.Client.SendMessage(ServerCreateRoomCommand.CommandId, sendingContent);
    }
  }
}
