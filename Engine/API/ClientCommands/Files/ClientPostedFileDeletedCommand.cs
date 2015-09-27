using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Linq;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientPostedFileDeletedCommand :
    ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.PostedFileDeleted;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ClientCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (receivedContent.File == null)
        throw new ArgumentNullException("file");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("roomName");

      using (var client = ClientModel.Get())
      {
        var downloadFiles = client.DownloadingFiles.Where((dFile) => dFile.File.Equals(receivedContent.File));

        foreach (var file in downloadFiles)
          file.Dispose();
      }

      var downloadEventArgs = new FileDownloadEventArgs
      {
        File = receivedContent.File,
        Progress = 0,
        RoomName = receivedContent.RoomName,
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
