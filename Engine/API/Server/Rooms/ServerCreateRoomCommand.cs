using Engine.Api.Client;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Linq;
using System.Security;

namespace Engine.Api.Server
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
    protected override void OnRun(MessageContent content, ServerCommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentNullException("RoomName");

      using(var server = ServerModel.Get())
      {
        if (server.Rooms.ContainsKey(content.RoomName))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAlreadyExist);
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
          Users = ServerModel.Api.GetRoomUsers(server, creatingRoom)
        };

        ServerModel.Server.SendMessage(args.ConnectionId, ClientRoomOpenedCommand.CommandId, sendingContent);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private string _roomName;
      private RoomType _type;

      public string RoomName
      {
        get { return _roomName; }
        set { _roomName = value; }
      }

      public RoomType Type
      {
        get { return _type; }
        set { _type = value; }
      }
    }
  }
}
