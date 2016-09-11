using Engine.Api.Server;
using Engine.Model.Client;
using System;
using System.IO;
using System.Security;

namespace Engine.Api.Client.Files
{
  [Serializable]
  public class ClientAddFileAction : IAction
  {
    private readonly string _roomName;
    private readonly string _fileName;

    /// <summary>
    /// Add file to room.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    /// <param name="path">Full path to file.</param>
    [SecuritySafeCritical]
    public ClientAddFileAction(string roomName, string fileName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (string.IsNullOrEmpty(fileName))
        throw new ArgumentException("fileName");

      _roomName = roomName;
      _fileName = fileName;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var info = new FileInfo(_fileName);
      if (!info.Exists)
        throw new InvalidOperationException("File not exist.");

      using (var client = ClientModel.Get())
      {
        var posted = client.Chat.GetOrCreatePostedFile(info, _roomName);
        var sendingContent = new ServerAddFileToRoomCommand.MessageContent { RoomName = _roomName, File = posted.File };
        ClientModel.Client.SendMessage(ServerAddFileToRoomCommand.CommandId, sendingContent);
      }
    }
  }
}
