using Engine.Api.Server.Files;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Api.Client.Files
{
  [Serializable]
  public class ClientRemoveFileAction : IAction
  {
    private readonly string _roomName;
    private readonly FileId _fileId;

    /// <summary>
    /// Remove posted file from room.
    /// </summary>
    /// <param name="roomName">Room name that contain removing file.</param>
    /// <param name="fileId">File identifier.</param>
    [SecuritySafeCritical]
    public ClientRemoveFileAction(string roomName, FileId fileId)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      _roomName = roomName;
      _fileId = fileId;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      using (var client = ClientModel.Get())
        client.Chat.RemovePostedFile(_roomName, _fileId);
      
      var sendingContent = new ServerRemoveFileFromRoomCommand.MessageContent { RoomName = _roomName, FileId = _fileId };
      ClientModel.Client.SendMessage(ServerRemoveFileFromRoomCommand.CommandId, sendingContent);
    }
  }
}
