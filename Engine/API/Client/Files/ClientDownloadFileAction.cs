using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using Engine.Network;
using System;
using System.IO;
using System.Security;

namespace Engine.Api.Client.Files
{
  [Serializable]
  public class ClientDownloadFileAction : IAction
  {
    private readonly string _roomName;
    private readonly FileId _fileId;
    private readonly string _savePath;

    /// <summary>
    /// Download file.
    /// </summary>
    /// <param name="roomName">Room name where file was posted.</param>
    /// <param name="fileId">File identifier.</param>
    /// <param name="savePath">Path for file saving.</param>
    [SecuritySafeCritical]
    public ClientDownloadFileAction(string roomName, FileId fileId, string savePath)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (string.IsNullOrEmpty(savePath))
        throw new ArgumentException("savePath");

      _roomName = roomName;
      _fileId = fileId;
      _savePath = savePath;
    }

    [SecuritySafeCritical]
    public void Pefrorm()
    {
      if (File.Exists(_savePath))
        throw new InvalidOperationException("File already exist.");

      using (var client = ClientModel.Get())
      {
        var chat = client.Chat;
        var room = chat.GetRoom(_roomName);

        if (chat.IsFileDownloading(_fileId))
          throw new ModelException(ErrorCode.FileAlreadyDownloading, _fileId);

        var file = room.TryGetFile(_fileId);
        if (file == null)
          throw new ModelException(ErrorCode.FileInRoomNotFound);

        if (chat.User.Equals(file.Id.Owner))
          throw new ModelException(ErrorCode.CantDownloadOwnFile);

        chat.AddFileDownload(new DownloadingFile(file, _savePath));

        var sendingContent = new ClientReadFilePartCommand.MessageContent
        {
          File = file,
          Length = AsyncClient.DefaultFilePartSize,
          RoomName = _roomName,
          StartPartPosition = 0,
        };

        ClientModel.Peer.SendMessage(file.Id.Owner, ClientReadFilePartCommand.CommandId, sendingContent);
      }
    }
  }
}
