using Engine.API.StandardAPI.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Linq;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerCreateRoomCommand :
      BaseServerCommand,
      IServerAPICommand
  {
    public void Run(ServerCommandArgs args)
    {
      MessageContent receivedContent = GetContentFromMessage<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentNullException("RoomName");

      using(var server = ServerModel.Get())
      {
        if (server.Rooms.ContainsKey(receivedContent.RoomName))
        {
          ServerModel.API.SendSystemMessage(args.ConnectionId, "Комната с таким именем уже создана, выберите другое имя.");
          return;
        }

        Room creatingRoom = receivedContent.Type == RoomType.Chat
          ? new Room(args.ConnectionId, receivedContent.RoomName)
          : new VoiceRoom(args.ConnectionId, receivedContent.RoomName);

        server.Rooms.Add(receivedContent.RoomName, creatingRoom);

        var sendingContent = new ClientRoomOpenedCommand.MessageContent 
        {
          Room = creatingRoom,
          Type = receivedContent.Type,
          Users = creatingRoom.Users.Select(nick => server.Users[nick]).ToList()
        };

        ServerModel.Server.SendMessage(args.ConnectionId, ClientRoomOpenedCommand.Id, sendingContent);
      }
    }

    [Serializable]
    public class MessageContent
    {
      string roomName;
      RoomType type;

      public string RoomName { get { return roomName; } set { roomName = value; } }
      public RoomType Type { get { return type; } set { type = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.CreateRoom;
  }
}
