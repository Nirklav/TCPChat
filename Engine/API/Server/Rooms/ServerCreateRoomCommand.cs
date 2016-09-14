using Engine.Api.Client;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using System;
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
        throw new ArgumentNullException("content.RoomName");

      using(var server = ServerModel.Get())
      {
        if (server.Chat.IsRoomExist(content.RoomName))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAlreadyExist);
          return;
        }

        Room room = null;
        if (content.Type == RoomType.Chat)
        {
          var textRoom = new ServerRoom(args.ConnectionId, content.RoomName);
          server.Chat.AddRoom(textRoom);
          room = textRoom;
        }

        if (content.Type == RoomType.Voice)
        {
          var voiceRoom = new ServerVoiceRoom(args.ConnectionId, content.RoomName);
          server.Chat.AddVoiceRoom(voiceRoom);
          room = voiceRoom;
        }

        if (room == null)
          throw new ArgumentException("content.RoomType");

        var sendingContent = new ClientRoomOpenedCommand.MessageContent 
        {
          Room = room.ToDto(args.ConnectionId),
          Users = server.Chat.GetRoomUserDtos(room.Name)
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
