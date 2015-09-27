using Engine.API.ClientCommands;
using Engine.Helpers;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Linq;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerCreateRoomCommand :
    BaseServerCommand,
    ICommand<ServerCommandArgs>
  {
    public const ushort CommandId = (ushort)ServerCommand.CreateRoom;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ServerCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentNullException("RoomName");

      using(var server = ServerModel.Get())
      {
        if (server.Rooms.ContainsKey(receivedContent.RoomName))
        {
          ServerModel.API.SendSystemMessage(args.ConnectionId, "Комната с таким именем уже создана, выберите другое имя.");
          return;
        }

        var creatingRoom = receivedContent.Type == RoomType.Chat
          ? new Room(args.ConnectionId, receivedContent.RoomName)
          : new VoiceRoom(args.ConnectionId, receivedContent.RoomName);

        server.Rooms.Add(receivedContent.RoomName, creatingRoom);

        var sendingContent = new ClientRoomOpenedCommand.MessageContent 
        {
          Room = creatingRoom,
          Type = receivedContent.Type,
          Users = creatingRoom.Users.Select(nick => server.Users[nick]).ToList()
        };

        ServerModel.Server.SendMessage(args.ConnectionId, ClientRoomOpenedCommand.CommandId, sendingContent);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private string roomName;
      private RoomType type;

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }

      public RoomType Type
      {
        get { return type; }
        set { type = value; }
      }
    }
  }
}
