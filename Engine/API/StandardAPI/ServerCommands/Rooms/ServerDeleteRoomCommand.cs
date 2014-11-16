using Engine.API.StandardAPI.ClientCommands;
using Engine.Helpers;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerDeleteRoomCommand :
      BaseServerCommand,
      ICommand<ServerCommandArgs>
  {
    public void Run(ServerCommandArgs args)
    {
      MessageContent receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("RoomName");

      if (string.Equals(receivedContent.RoomName, ServerModel.MainRoomName))
      {
        ServerModel.API.SendSystemMessage(args.ConnectionId, "Вы не можете удалить основную комнату.");
        return;
      }

      if (!RoomExists(receivedContent.RoomName, args.ConnectionId))
        return;

      using (var context = ServerModel.Get())
      {
        Room deletingRoom = context.Rooms[receivedContent.RoomName];
        if (!deletingRoom.Admin.Equals(args.ConnectionId))
        {
          ServerModel.API.SendSystemMessage(args.ConnectionId, "Вы не являетесь администратором комнаты. Операция отменена.");
          return;
        }

        context.Rooms.Remove(deletingRoom.Name);

        var sendingContent = new ClientRoomClosedCommand.MessageContent { Room = deletingRoom };
        foreach (string user in deletingRoom.Users)
          ServerModel.Server.SendMessage(user, ClientRoomClosedCommand.Id, sendingContent);
      }
    }

    [Serializable]
    public class MessageContent
    {
      string roomName;

      public string RoomName { get { return roomName; } set { roomName = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.DeleteRoom;
  }
}
