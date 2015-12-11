using Engine.API.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Linq;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerCreateRoomCommand :
    ServerCommand<ServerCreateRoomCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.CreateRoom;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public override void Run(MessageContent content, ServerCommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentNullException("RoomName");

      using(var server = ServerModel.Get())
      {
        if (server.Rooms.ContainsKey(content.RoomName))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, "Комната с таким именем уже создана, выберите другое имя.");
          return;
        }

        var creatingRoom = content.Type == RoomType.Chat
          ? new Room(args.ConnectionId, content.RoomName)
          : new VoiceRoom(args.ConnectionId, content.RoomName);

        server.Rooms.Add(content.RoomName, creatingRoom);

        var sendingContent = new ClientRoomOpenedCommand.MessageContent 
        {
          Room = creatingRoom,
          Type = content.Type,
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
