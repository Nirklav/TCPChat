using Engine.API.ClientCommands;
using Engine.Helpers;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Linq;

namespace Engine.API.ServerCommands
{
  class ServerRefreshRoomCommand :
      BaseServerCommand,
      ICommand<ServerCommandArgs>
  {
    public void Run(ServerCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("RoomName");

      if (string.Equals(receivedContent.RoomName, ServerModel.MainRoomName))
        return;

      if (!RoomExists(receivedContent.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[receivedContent.RoomName];

        if (!room.Users.Contains(args.ConnectionId))
        {
          ServerModel.API.SendSystemMessage(args.ConnectionId, "Вы не входите в состав этой комнаты.");
          return;
        }

        var roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent
        {
          Room = room,
          Users = room.Users.Select(nick => server.Users[nick]).ToList()
        };
        ServerModel.Server.SendMessage(args.ConnectionId, ClientRoomRefreshedCommand.Id, roomRefreshedContent);
      }
    }

    [Serializable]
    public class MessageContent
    {
      string roomName;

      public string RoomName { get { return roomName; } set { roomName = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.RefreshRoom;
  }
}
