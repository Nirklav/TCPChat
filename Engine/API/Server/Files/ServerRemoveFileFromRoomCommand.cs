using Engine.Api.Client;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
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
        throw new ArgumentException("content.RoomName");

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server.Chat, content.RoomName, args.ConnectionId, out room))
          return;

        var file = room.TryGetFile(content.FileId);
        if (file == null)
          return;

        if (!room.IsUserExist(args.ConnectionId))
        {
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
          return;
        }

        if (room.Admin != args.ConnectionId && file.Id.Owner != args.ConnectionId)
        {
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.FileRemoveAccessDenied));
          return;
        }

        room.RemoveFile(file.Id);
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.FileRemoved, file.Name));

        var postedFileDeletedContent = new ClientFileRemovedCommand.MessageContent
        {
          RoomName = room.Name,
          FileId = file.Id
        };

        foreach (string user in room.Users)
          ServerModel.Server.SendMessage(user, ClientFileRemovedCommand.CommandId, postedFileDeletedContent);
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
