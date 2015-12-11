using Engine.Model.Client;
using Engine.Model.Entities;
using Engine.Network;
using System;
using System.IO;
using System.Linq;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientWriteFilePartCommand :
    ClientCommand<ClientWriteFilePartCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.WriteFilePart;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public override void Run(MessageContent content, ClientCommandArgs args)
    {
      if (args.PeerConnectionId == null)
        return;

      if (content.File == null)
        throw new ArgumentNullException("File");

      if (content.Part == null)
        throw new ArgumentNullException("Part");

      if (content.StartPartPosition < 0)
        throw new ArgumentException("StartPartPosition < 0");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("roomName");

      var downloadEventArgs = new FileDownloadEventArgs
      {
        RoomName = content.RoomName,
        File = content.File,
      };

      using (var client = ClientModel.Get())
      {
        var downloadingFile = client.DownloadingFiles.FirstOrDefault((current) => current.File.Equals(content.File));
        if (downloadingFile == null)
          return;

        if (downloadingFile.WriteStream == null)
          downloadingFile.WriteStream = File.Create(downloadingFile.FullName);

        if (downloadingFile.WriteStream.Position == content.StartPartPosition)
          downloadingFile.WriteStream.Write(content.Part, 0, content.Part.Length);

        downloadingFile.File = content.File;

        if (downloadingFile.WriteStream.Position >= content.File.Size)
        {
          client.DownloadingFiles.Remove(downloadingFile);
          downloadingFile.WriteStream.Dispose();
          downloadEventArgs.Progress = 100;
        }
        else
        {
          var sendingContent = new ClientReadFilePartCommand.MessageContent
          {
            File = content.File,
            Length = AsyncClient.DefaultFilePartSize,
            RoomName = content.RoomName,
            StartPartPosition = downloadingFile.WriteStream.Position,
          };

          ClientModel.Peer.SendMessage(args.PeerConnectionId, ClientReadFilePartCommand.CommandId, sendingContent);
          downloadEventArgs.Progress = (int)((downloadingFile.WriteStream.Position * 100) / content.File.Size);
        }
      }

      ClientModel.Notifier.DownloadProgress(downloadEventArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private FileDescription file;
      private string roomName;
      private long startPartPosition;
      private byte[] part;

      public FileDescription File
      {
        get { return file; }
        set { file = value; }
      }

      public long StartPartPosition
      {
        get { return startPartPosition; }
        set { startPartPosition = value; }
      }

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }

      public byte[] Part
      {
        get { return part; }
        set { part = value; }
      }
    }
  }
}
