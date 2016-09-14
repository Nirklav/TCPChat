using Engine.Api.Server;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Api.Client
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

    protected override bool IsPeerCommand
    {
      [SecuritySafeCritical]
      get { return true; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (content.File == null)
        throw new ArgumentNullException("content.File");

      if (content.Length <= 0)
        throw new ArgumentException("content.Length <= 0");

      if (content.StartPartPosition < 0)
        throw new ArgumentException("content.StartPartPosition < 0");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("content.RoomName");

      using(var client = ClientModel.Get())
      {
        var posted = client.Chat.TryGetPostedFile(content.File.Id);
        if (posted == null)
        {
          SendFileNotPost(content.RoomName, content.File.Id);
          return;
        }

        var room = client.Chat.GetRoom(content.RoomName);
        if (!room.IsUserExist(args.PeerConnectionId))
        {
          SendFileNotPost(content.RoomName, content.File.Id);
          return;
        }

        if (!posted.RoomNames.Contains(content.RoomName))
        {
          SendFileNotPost(content.RoomName, content.File.Id);
          return;
        }

        var sendingContent = new ClientWriteFilePartCommand.MessageContent
        {
          File = content.File,
          StartPartPosition = content.StartPartPosition,
          RoomName = content.RoomName,
        };

        var partSize = content.File.Size < content.StartPartPosition + content.Length
          ? content.File.Size - content.StartPartPosition
          : content.Length;

        var part = new byte[partSize];
        posted.ReadStream.Position = content.StartPartPosition;
        posted.ReadStream.Read(part, 0, part.Length);

        ClientModel.Peer.SendMessage(args.PeerConnectionId, ClientWriteFilePartCommand.CommandId, sendingContent, part);
      }
    }

    private static void SendFileNotPost(string roomName, FileId fileId)
    {
      var fileNotPostContent = new ServerRemoveFileFromRoomCommand.MessageContent
      {
        FileId = fileId,
        RoomName = roomName,
      };

      ClientModel.Client.SendMessage(ServerRemoveFileFromRoomCommand.CommandId, fileNotPostContent);
    }

    [Serializable]
    public class MessageContent
    {
      private string _roomName;
      private FileDescription _file;
      private long _startPartPosition;
      private long _length;

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

      public long Length
      {
        get { return _length; }
        set { _length = value; }
      }
    }
  }
}
