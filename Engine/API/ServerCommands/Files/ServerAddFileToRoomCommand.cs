using Engine.API.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Linq;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerAddFileToRoomCommand :
    ServerCommand<ServerAddFileToRoomCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.AddFileToRoom;

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

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server, content.RoomName, args.ConnectionId, out room))
          return;

        if (!room.Users.Contains(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAccessDenied);
          return;
        }

        if (!room.Files.Exists(f => f.Equals(content.File)))
          room.Files.Add(content.File);

        var sendingContent = new ClientFilePostedCommand.MessageContent
        {
          File = content.File,
          RoomName = content.RoomName
        };

        foreach (string user in room.Users)
          ServerModel.Server.SendMessage(user, ClientFilePostedCommand.CommandId, sendingContent);
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
