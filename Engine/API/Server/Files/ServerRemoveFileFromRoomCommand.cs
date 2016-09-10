using Engine.Api.Client;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.Api.Server
{
  [SecurityCritical]
  class ServerRemoveFileFromRoomCommand :
    ServerCommand<ServerRemoveFileFromRoomCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.RemoveFileFromRoom;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ServerCommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("RoomName");

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server, content.RoomName, args.ConnectionId, out room))
          return;

        var file = room.Files.Find(f => f.Id == content.FileId);
        if (file == null)
          return;

        if (!room.ContainsUser(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAccessDenied);
          return;
        }

        if (room.Admin != args.ConnectionId && file.Id.Owner != args.ConnectionId)
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.FileRemoveAccessDenied);
          return;
        }

        room.Files.Remove(file);
        ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.FileRemoved, file.Name);

        var postedFileDeletedContent = new ClientPostedFileDeletedCommand.MessageContent
        {
          RoomName = room.Name,
          FileId = file.Id
        };

        foreach (string user in room.Users)
          ServerModel.Server.SendMessage(user, ClientPostedFileDeletedCommand.CommandId, postedFileDeletedContent);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private string _roomName;
      private FileId _fileId;

      public string RoomName
      {
        get { return _roomName; }
        set { _roomName = value; }
      }

      public FileId FileId
      {
        get { return _fileId; }
        set { _fileId = value; }
      }
    }
  }
}
