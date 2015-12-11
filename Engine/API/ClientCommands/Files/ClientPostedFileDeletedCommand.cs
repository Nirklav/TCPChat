using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Linq;
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
      if (content.File == null)
        throw new ArgumentNullException("file");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("roomName");

      using (var client = ClientModel.Get())
      {
        var downloadFiles = client.DownloadingFiles.Where((dFile) => dFile.File.Equals(content.File));

        foreach (var file in downloadFiles)
          file.Dispose();
      }

      var downloadEventArgs = new FileDownloadEventArgs
      {
        File = content.File,
        Progress = 0,
        RoomName = content.RoomName,
      };

      ClientModel.Notifier.PostedFileDeleted(downloadEventArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private FileDescription file;
      private string roomName;

      public FileDescription File
      {
        get { return file; }
        set { file = value; }
      }

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }
    }
  }
}
