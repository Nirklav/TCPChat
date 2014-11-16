using Engine.API.StandardAPI.ClientCommands;
using Engine.Helpers;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Linq;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerSendRoomMessageCommand :
      BaseServerCommand,
      ICommand<ServerCommandArgs>
  {
    public void Run(ServerCommandArgs args)
    {
      MessageContent receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.Message))
        throw new ArgumentException("Message");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentNullException("RoomName");

      var sendingContent = new ClientOutRoomMessageCommand.MessageContent
      {
        Message = receivedContent.Message,
        RoomName = receivedContent.RoomName,
        Sender = args.ConnectionId,
      };

      if (!RoomExists(receivedContent.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        Room room = server.Rooms[receivedContent.RoomName];

        if (!room.Users.Contains(args.ConnectionId))
        {
          ServerModel.API.SendSystemMessage(args.ConnectionId, "Вы не можете отправить сообщение, т.к. не входите в состав этой комнаты.");
          return;
        }

        foreach (string user in room.Users.Where(u => u != null))
          ServerModel.Server.SendMessage(user, ClientOutRoomMessageCommand.Id, sendingContent);
      }
    }

    [Serializable]
    public class MessageContent
    {
      string message;
      string roomName;

      public string Message { get { return message; } set { message = value; } }
      public string RoomName { get { return roomName; } set { roomName = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.SendRoomMessage;
  }
}
