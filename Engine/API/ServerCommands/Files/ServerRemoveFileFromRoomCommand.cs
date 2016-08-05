using Engine.API.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.API.ServerCommands
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

        if (room.Admin != args.ConnectionId && file.Owner != args.ConnectionId)
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.FileRemoveAccessDenied);
          return;
        }

        room.Files.Remove(file);
        ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.FileRemoved, file.Name);

        var postedFileDeletedContent = new ClientPostedFileDeletedCommand.MessageContent()
        {
          FileId = file.Id,
          RoomName = room.Name,
        };

        foreach (string user in room.Users)
          ServerModel.Server.SendMessage(user, ClientPostedFileDeletedCommand.CommandId, postedFileDeletedContent);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private string roomName;
      private int fileId;

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }

      public int FileId
      {
        get { return fileId; }
        set { fileId = value; }
      }
    }
  }
}
