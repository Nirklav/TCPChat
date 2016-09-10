using Engine.Model.Client;
using Engine.Model.Common.Entities;
using Engine.Network;
using System;
using System.Security;

namespace Engine.Api.Client
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

      var fileId = content.File.Id;
      var downloadEventArgs = new FileDownloadEventArgs
      {
        RoomName = content.RoomName,
        FileId = fileId
      };

      using (var client = ClientModel.Get())
      {
        var chat = client.Chat;

        var downloading = chat.TryGetFileDownload(fileId);
        if (downloading == null)
          return;

        var stream = downloading.WriteStream;
        var filePart = args.Unpacked.RawData;
        var filePartLength = args.Unpacked.RawLength;

        if (stream.Position == content.StartPartPosition)
          stream.Write(filePart, 0, filePartLength);

        if (stream.Position >= content.File.Size)
        {
          chat.RemoveFileDownload(fileId);

          downloadEventArgs.Progress = 100;
        }
        else
        {
          var sendingContent = new ClientReadFilePartCommand.MessageContent
          {
            File = content.File,
            Length = AsyncClient.DefaultFilePartSize,
            RoomName = content.RoomName,
            StartPartPosition = stream.Position,
          };

          ClientModel.Peer.SendMessage(args.PeerConnectionId, ClientReadFilePartCommand.CommandId, sendingContent);

          downloadEventArgs.Progress = (int)((stream.Position * 100) / content.File.Size);
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
