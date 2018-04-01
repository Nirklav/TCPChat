using System;
using System.Security;
using Engine.Api.Client.Files;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Server.Files
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
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException(nameof(content.RoomName));

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

        foreach (var userId in room.Users)
          ServerModel.Server.SendMessage(userId, ClientFileRemovedCommand.CommandId, postedFileDeletedContent);
      }
    }

    [Serializable]
    [BinType("ServerRemoveFileFromRoom")]
    public class MessageContent
    {
      [BinField("r")]
      public string RoomName;

      [BinField("f")]
      public FileId FileId;
    }
  }
}
