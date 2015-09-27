using Engine.API.ServerCommands;
using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Linq;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientReadFilePartCommand :
    ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.ReadFilePart;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ClientCommandArgs args)
    {
      if (args.PeerConnectionId == null)
        return;

      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

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

          ClientModel.Client.SendMessage(ServerRemoveFileFromRoomCommand.CommandId, fileNotPostContent);
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
        var sendingFileStream = client.PostedFiles.First(c => c.File.Equals(receivedContent.File)).ReadStream;
        sendingFileStream.Position = receivedContent.StartPartPosition;
        sendingFileStream.Read(sendingContent.Part, 0, sendingContent.Part.Length);
      }

      ClientModel.Peer.SendMessage(args.PeerConnectionId, ClientWriteFilePartCommand.CommandId, sendingContent);
    }

    [Serializable]
    public class MessageContent
    {
      private FileDescription file;
      private long startPartPosition;
      private long length;
      private string roomName;

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

      public long Length
      {
        get { return length; }
        set { length = value; }
      }

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }
    }
  }
}
