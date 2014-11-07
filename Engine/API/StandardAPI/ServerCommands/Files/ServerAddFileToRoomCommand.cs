using Engine.API.StandardAPI.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network.Connections;
using System;
using System.Linq;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerAddFileToRoomCommand :
      BaseServerCommand,
      IServerCommand
  {
    public void Run(ServerCommandArgs args)
    {
      MessageContent receivedContent = GetContentFromMessage<MessageContent>(args.Message);

      if (receivedContent.File == null)
        throw new ArgumentNullException("File");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("RoomName");

      if (!RoomExists(receivedContent.RoomName, args.ConnectionId))
        return;

      using (var context = ServerModel.Get())
      {
        Room room = context.Rooms[receivedContent.RoomName];

        if (!room.Users.Contains(args.ConnectionId))
        {
          ServerModel.API.SendSystemMessage(args.ConnectionId, "Вы не входите в состав этой комнаты.");
          return;
        }

        if (room.Files.FirstOrDefault(file => file.Equals(receivedContent.File)) == null)
          room.Files.Add(receivedContent.File);

        var sendingContent = new ClientFilePostedCommand.MessageContent
        {
          File = receivedContent.File,
          RoomName = receivedContent.RoomName
        };

        foreach (string user in room.Users)
          ServerModel.Server.SendMessage(user, ClientFilePostedCommand.Id, sendingContent);
      }
    }

    [Serializable]
    public class MessageContent
    {
      string roomName;
      FileDescription file;

      public string RoomName { get { return roomName; } set { roomName = value; } }
      public FileDescription File { get { return file; } set { file = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.AddFileToRoom;
  }
}
