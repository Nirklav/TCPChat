using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Collections.Generic;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientPostedFileDeletedCommand :
    ClientCommand<ClientPostedFileDeletedCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.PostedFileDeleted;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("roomName");

      using (var client = ClientModel.Get())
      {
        // Remove file from room
        Room room;
        if (client.Rooms.TryGetValue(content.RoomName, out room))
          room.Files.RemoveAll(f => f.Id == content.FileId);

        // Remove downloading files
        var removed = new List<DownloadingFile>();
        client.DownloadingFiles.RemoveAll(f =>
        {
          if (f.File.Id == content.FileId)
          {
            removed.Add(f);
            return true;
          }
          return false;
        });

        foreach (var file in removed)
          file.Dispose();
      }

      var downloadEventArgs = new FileDownloadEventArgs
      {
        FileId = content.FileId,
        Progress = 0,
        RoomName = content.RoomName,
      };

      ClientModel.Notifier.PostedFileDeleted(downloadEventArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private int fileId;
      private string roomName;

      public int FileId
      {
        get { return fileId; }
        set { fileId = value; }
      }

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }
    }
  }
}
