using System;
using System.Security;
using Engine.Api.Client.Rooms;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Server.Rooms
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
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentNullException("content.RoomName");

      using(var server = ServerModel.Get())
      {
        if (server.Chat.IsRoomExist(content.RoomName))
        {
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAlreadyExist));
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
    [BinType("ServerCreateRoom")]
    public class MessageContent
    {
      [BinField("r")]
      public string RoomName;

      [BinField("t")]
      public RoomType Type;
    }
  }
}
