using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientPostedFileDeletedCommand :
      BaseCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.File == null)
        throw new ArgumentNullException("file");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("roomName");

      using (var client = ClientModel.Get())
      {
        IEnumerable<DownloadingFile> downloadFiles = client.DownloadingFiles.Where((dFile) => dFile.File.Equals(receivedContent.File));

        foreach (DownloadingFile file in downloadFiles)
          file.Dispose();
      }

      FileDownloadEventArgs downloadEventArgs = new FileDownloadEventArgs
      {
        File = receivedContent.File,
        Progress = 0,
        RoomName = receivedContent.RoomName,
      };

      ClientModel.OnPostedFileDeleted(this, downloadEventArgs);
    }

    [Serializable]
    public class MessageContent
    {
      FileDescription file;
      string roomName;

      public FileDescription File { get { return file; } set { file = value; } }
      public string RoomName { get { return roomName; } set { roomName = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.PostedFileDeleted;
  }
}
