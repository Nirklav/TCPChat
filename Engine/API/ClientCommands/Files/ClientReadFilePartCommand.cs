using Engine.API.ServerCommands;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Linq;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientReadFilePartCommand :
    ClientCommand<ClientReadFilePartCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.ReadFilePart;

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

      if (content.Length <= 0)
        throw new ArgumentException("Length <= 0");

      if (content.StartPartPosition < 0)
        throw new ArgumentException("StartPartPosition < 0");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("roomName");

      using(var client = ClientModel.Get())
      {
        if (!client.PostedFiles.Exists((current) => current.File.Equals(content.File)))
        {
          var fileNotPostContent = new ServerRemoveFileFromRoomCommand.MessageContent
          {
            File = content.File,
            RoomName = content.RoomName,
          };

          ClientModel.Client.SendMessage(ServerRemoveFileFromRoomCommand.CommandId, fileNotPostContent);
          return;
        }
      }

      var sendingContent = new ClientWriteFilePartCommand.MessageContent
      {
        File = content.File,
        StartPartPosition = content.StartPartPosition,
        RoomName = content.RoomName,
      };

      long partSize;
      if (content.File.Size < content.StartPartPosition + content.Length)
        partSize = content.File.Size - content.StartPartPosition;
      else
        partSize = content.Length;

      sendingContent.Part = new byte[partSize];

      using (var client = ClientModel.Get())
      {
        var sendingFileStream = client.PostedFiles.First(c => c.File.Equals(content.File)).ReadStream;
        sendingFileStream.Position = content.StartPartPosition;
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
