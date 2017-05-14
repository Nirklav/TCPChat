using System;
using System.Security;
using Engine.Model.Client;
using Engine.Model.Common.Dto;
using Engine.Network;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.Files
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
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (content.File == null)
        throw new ArgumentNullException("content.File");

      if (args.Unpacked.RawData == null)
        throw new ArgumentNullException("args.Unpacked.RawData");

      if (args.Unpacked.RawLength <= 0)
        throw new ArgumentException("args.Unpacked.RawLength <= 0");

      if (content.StartPartPosition < 0)
        throw new ArgumentException("content.StartPartPosition < 0");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("content.RoomName");

      var fileId = content.File.Id;
      var progress = 0;

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

          progress = 100;
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

          ClientModel.Peer.SendMessage(args.ConnectionId, ClientReadFilePartCommand.CommandId, sendingContent);

          progress = (int)((stream.Position * 100) / content.File.Size);
        }
      }

      ClientModel.Notifier.DownloadProgress(new FileDownloadEventArgs(content.RoomName, fileId, progress));
    }

    [Serializable]
    [BinType("ClientWriteFilePart")]
    public class MessageContent
    {
      [BinField("r")]
      public string RoomName;

      [BinField("f")]
      public FileDescriptionDto File;

      [BinField("p")]
      public long StartPartPosition;
    }
  }
}
