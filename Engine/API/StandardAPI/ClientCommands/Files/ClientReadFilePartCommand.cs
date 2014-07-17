using Engine.API.StandardAPI.ServerCommands;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.IO;
using System.Linq;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientReadFilePartCommand :
      BaseCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      if (args.PeerConnectionId == null)
        return;

      MessageContent receivedContent = GetContentFromMessage<MessageContent>(args.Message);

      if (receivedContent.File == null)
        throw new ArgumentNullException("File");

      if (receivedContent.Length <= 0)
        throw new ArgumentException("Length <= 0");

      if (receivedContent.StartPartPosition < 0)
        throw new ArgumentException("StartPartPosition < 0");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("roomName");

      using(var client = ClientModel.Get())
      {
        if (!client.PostedFiles.Exists((current) => current.File.Equals(receivedContent.File)))
        {
          var fileNotPostContent = new ServerRemoveFileFromRoomCommand.MessageContent
          {
            File = receivedContent.File,
            RoomName = receivedContent.RoomName,
          };

          ClientModel.Client.SendMessage(ServerRemoveFileFromRoomCommand.Id, fileNotPostContent);
          return;
        }
      }

      var sendingContent = new ClientWriteFilePartCommand.MessageContent
      {
        File = receivedContent.File,
        StartPartPosition = receivedContent.StartPartPosition,
        RoomName = receivedContent.RoomName,
      };

      long partSize;
      if (receivedContent.File.Size < receivedContent.StartPartPosition + receivedContent.Length)
        partSize = receivedContent.File.Size - receivedContent.StartPartPosition;
      else
        partSize = receivedContent.Length;

      sendingContent.Part = new byte[partSize];

      using (var client = ClientModel.Get())
      {
        FileStream sendingFileStream = client.PostedFiles.First(c => c.File.Equals(receivedContent.File)).ReadStream;
        sendingFileStream.Position = receivedContent.StartPartPosition;
        sendingFileStream.Read(sendingContent.Part, 0, sendingContent.Part.Length);
      }

      ClientModel.Peer.SendMessage(args.PeerConnectionId, ClientWriteFilePartCommand.Id, sendingContent);
    }

    [Serializable]
    public class MessageContent
    {
      FileDescription file;
      long startPartPosition;
      long length;
      string roomName;

      public FileDescription File { get { return file; } set { file = value; } }
      public long StartPartPosition { get { return startPartPosition; } set { startPartPosition = value; } }
      public long Length { get { return length; } set { length = value; } }
      public string RoomName { get { return roomName; } set { roomName = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.ReadFilePart;
  }
}
