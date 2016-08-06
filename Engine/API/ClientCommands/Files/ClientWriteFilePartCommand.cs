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

    protected override bool IsPeerCommand
    {
      [SecuritySafeCritical]
      get { return true; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
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
        FileId = content.File.Id
      };

      using (var client = ClientModel.Get())
      {
        var file = client.DownloadingFiles.Find(f => f.File.Equals(content.File));
        if (file == null)
          return;

        if (file.WriteStream == null)
          file.WriteStream = File.Create(file.FullName);

        if (file.WriteStream.Position == content.StartPartPosition)
          file.WriteStream.Write(content.Part, 0, content.Part.Length);

        file.File = content.File;

        if (file.WriteStream.Position >= content.File.Size)
        {
          client.DownloadingFiles.Remove(file);
          file.WriteStream.Dispose();
          downloadEventArgs.Progress = 100;
        }
        else
        {
          var sendingContent = new ClientReadFilePartCommand.MessageContent
          {
            File = content.File,
            Length = AsyncClient.DefaultFilePartSize,
            RoomName = content.RoomName,
            StartPartPosition = file.WriteStream.Position,
          };

          ClientModel.Peer.SendMessage(args.PeerConnectionId, ClientReadFilePartCommand.CommandId, sendingContent);
          downloadEventArgs.Progress = (int)((file.WriteStream.Position * 100) / content.File.Size);
        }
      }

      ClientModel.Notifier.DownloadProgress(downloadEventArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private string roomName;
      private FileDescription file;
      private long startPartPosition;
      private byte[] part;

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }

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

      public byte[] Part
      {
        get { return part; }
        set { part = value; }
      }
    }
  }
}
