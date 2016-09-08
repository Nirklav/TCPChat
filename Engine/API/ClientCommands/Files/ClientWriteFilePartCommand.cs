using Engine.Model.Client;
using Engine.Model.Entities;
using Engine.Network;
using System;
using System.IO;
using System.Security;

namespace Engine.Api.ClientCommands
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

      if (args.Unpacked.RawData == null)
        throw new ArgumentNullException("args.Unpacked.RawData");

      if (args.Unpacked.RawLength <= 0)
        throw new ArgumentException("args.Unpacked.RawLength <= 0");

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

        var filePart = args.Unpacked.RawData;
        var filePartLength = args.Unpacked.RawLength;

        if (file.WriteStream.Position == content.StartPartPosition)
          file.WriteStream.Write(filePart, 0, filePartLength);

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
      private string _roomName;
      private FileDescription _file;
      private long _startPartPosition;

      public string RoomName
      {
        get { return _roomName; }
        set { _roomName = value; }
      }

      public FileDescription File
      {
        get { return _file; }
        set { _file = value; }
      }

      public long StartPartPosition
      {
        get { return _startPartPosition; }
        set { _startPartPosition = value; }
      }
    }
  }
}
