using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using Engine.Network;
using System;
using System.IO;
using System.Linq;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientWriteFilePartCommand :
      ICommand<ClientCommandArgs>
  {
    public void Run(ClientCommandArgs args)
    {
      if (args.PeerConnectionId == null)
        return;

      MessageContent receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (receivedContent.File == null)
        throw new ArgumentNullException("File");

      if (receivedContent.Part == null)
        throw new ArgumentNullException("Part");

      if (receivedContent.StartPartPosition < 0)
        throw new ArgumentException("StartPartPosition < 0");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("roomName");

      FileDownloadEventArgs downloadEventArgs = new FileDownloadEventArgs
      {
        RoomName = receivedContent.RoomName,
        File = receivedContent.File,
      };

      using (var client = ClientModel.Get())
      {
        DownloadingFile downloadingFile = client.DownloadingFiles.FirstOrDefault((current) => current.File.Equals(receivedContent.File));

        if (downloadingFile == null)
          return;

        if (downloadingFile.WriteStream == null)
          downloadingFile.WriteStream = File.Create(downloadingFile.FullName);

        if (downloadingFile.WriteStream.Position == receivedContent.StartPartPosition)
          downloadingFile.WriteStream.Write(receivedContent.Part, 0, receivedContent.Part.Length);

        downloadingFile.File = receivedContent.File;

        if (downloadingFile.WriteStream.Position >= receivedContent.File.Size)
        {
          client.DownloadingFiles.Remove(downloadingFile);
          downloadingFile.WriteStream.Dispose();
          downloadEventArgs.Progress = 100;
        }
        else
        {
          var sendingContent = new ClientReadFilePartCommand.MessageContent
          {
            File = receivedContent.File,
            Length = AsyncClient.DefaultFilePartSize,
            RoomName = receivedContent.RoomName,
            StartPartPosition = downloadingFile.WriteStream.Position,
          };

          ClientModel.Peer.SendMessage(args.PeerConnectionId, ClientReadFilePartCommand.Id, sendingContent);
          downloadEventArgs.Progress = (int)((downloadingFile.WriteStream.Position * 100) / receivedContent.File.Size);
        }
      }

      ClientModel.Notifier.DownloadProgress(downloadEventArgs);
    }

    [Serializable]
    public class MessageContent
    {
      FileDescription file;
      string roomName;
      long startPartPosition;
      byte[] part;

      public FileDescription File { get { return file; } set { file = value; } }
      public long StartPartPosition { get { return startPartPosition; } set { startPartPosition = value; } }
      public string RoomName { get { return roomName; } set { roomName = value; } }
      public byte[] Part { get { return part; } set { part = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.WriteFilePart;
  }
}
