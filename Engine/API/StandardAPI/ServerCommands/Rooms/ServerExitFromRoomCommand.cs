using Engine.API.StandardAPI.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network.Connections;
using System;
using System.Linq;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerExitFromRoomCommand :
      BaseServerCommand,
      IServerCommand
  {
    public void Run(ServerCommandArgs args)
    {
      MessageContent receivedContent = GetContentFromMessage<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("RoomName");

      if (string.Equals(receivedContent.RoomName, ServerModel.MainRoomName))
      {
        ServerModel.API.SendSystemMessage(args.ConnectionId, "Невозможно выйти из основной комнаты.");
        return;
      }

      if (!RoomExists(receivedContent.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        Room room = server.Rooms[receivedContent.RoomName];

        if (!room.Users.Contains(args.ConnectionId))
        {
          ServerModel.API.SendSystemMessage(args.ConnectionId, "Вы и так не входите в состав этой комнаты.");
          return;
        }

        room.Remove(args.ConnectionId);
        var closeRoomContent = new ClientRoomClosedCommand.MessageContent { Room = room };
        ServerModel.Server.SendMessage(args.ConnectionId, ClientRoomClosedCommand.Id, closeRoomContent);

        if (string.Equals(room.Admin, args.ConnectionId))
        {
          room.Admin = room.Users.FirstOrDefault();

          if (room.Admin != null)
          {
            string message = string.Format("Вы назначены администратором комнаты \"{0}\".", room.Name);
            ServerModel.API.SendSystemMessage(room.Admin, message);
          }
        }

        foreach (string user in room.Users)
        {
          var roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent
          {
            Room = room,
            Users = room.Users.Select(nick => server.Users[nick]).ToList()
          };
          ServerModel.Server.SendMessage(user, ClientRoomRefreshedCommand.Id, roomRefreshedContent);
        }
      }
    }

    [Serializable]
    public class MessageContent
    {
      string roomName;

      public string RoomName { get { return roomName; } set { roomName = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.ExitFromRoom;
  }
}
