using System;
using System.Security;
using Engine.Api.Server.Files;
using Engine.Model.Client;
using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.Files
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
    protected override void OnRun(MessageContent content, CommandArgs args)
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
        if (!room.IsUserExist(args.ConnectionId))
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

        ClientModel.Peer.SendMessage(args.ConnectionId, ClientWriteFilePartCommand.CommandId, sendingContent, part);
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
    [BinType("ClientReadFilePart")]
    public class MessageContent
    {
      [BinField("r")]
      public string RoomName;

      [BinField("f")]
      public FileDescriptionDto File;

      [BinField("p")]
      public long StartPartPosition;

      [BinField("l")]
      public long Length;
    }
  }
}
