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
      if (content.File == null)
        throw new ArgumentNullException("File");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("RoomName");

      if (!RoomExists(content.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[content.RoomName];

        if (!room.Files.Exists(file => file.Equals(content.File)))
          return;

        if (!room.ContainsUser(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, MessageId.RoomAccessDenied);
          return;
        }

        bool access = false;
        if (room.Admin != null)
          access |= args.ConnectionId.Equals(room.Admin);
        access |= args.ConnectionId.Equals(content.File.Owner.Nick);
        if (!access)
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, MessageId.FileRemoveAccessDenied);
          return;
        }

        room.Files.Remove(content.File);
        ServerModel.Api.SendSystemMessage(args.ConnectionId, MessageId.FileRemoved, content.File.Name);

        var postedFileDeletedContent = new ClientPostedFileDeletedCommand.MessageContent()
        {
          File = content.File,
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
      private FileDescription file;

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }

      public FileDescription File
      {
        get { return file; }
        set { file = value; }
      }
    }
  }
}
