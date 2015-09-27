using Engine.API.ClientCommands;
using Engine.Helpers;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerRemoveFileFromRoomCommand :
    BaseServerCommand,
    ICommand<ServerCommandArgs>
  {
    public const ushort CommandId = (ushort)ServerCommand.RemoveFileFromRoom;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ServerCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (receivedContent.File == null)
        throw new ArgumentNullException("File");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("RoomName");

      if (!RoomExists(receivedContent.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[receivedContent.RoomName];

        if (!room.Files.Exists(file => file.Equals(receivedContent.File)))
          return;

        if (!room.ContainsUser(args.ConnectionId))
        {
          ServerModel.API.SendSystemMessage(args.ConnectionId, "Вы не входите в состав этой комнаты.");
          return;
        }

        bool access = false;
        if (room.Admin != null)
          access |= args.ConnectionId.Equals(room.Admin);
        access |= args.ConnectionId.Equals(receivedContent.File.Owner.Nick);
        if (!access)
        {
          ServerModel.API.SendSystemMessage(args.ConnectionId, "Вы не можете удалить данный файл. Не хватает прав.");
          return;
        }

        room.Files.Remove(receivedContent.File);
        ServerModel.API.SendSystemMessage(args.ConnectionId, string.Format("Файл \"{0}\" удален с раздачи.", receivedContent.File.Name));

        foreach (string user in room.Users)
        {
          var postedFileDeletedContent = new ClientPostedFileDeletedCommand.MessageContent()
          {
            File = receivedContent.File,
            RoomName = room.Name,
          };
          ServerModel.Server.SendMessage(user, ClientPostedFileDeletedCommand.CommandId, postedFileDeletedContent);
        }
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
