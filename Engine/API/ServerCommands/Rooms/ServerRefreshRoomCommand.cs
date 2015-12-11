using Engine.API.ClientCommands;
using Engine.Model.Server;
using System;
using System.Linq;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerRefreshRoomCommand :
    ServerCommand<ServerRefreshRoomCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.RefreshRoom;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public override void Run(MessageContent content, ServerCommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("RoomName");

      if (string.Equals(content.RoomName, ServerModel.MainRoomName))
        return;

      if (!RoomExists(content.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[content.RoomName];

        if (!room.Users.Contains(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, "Вы не входите в состав этой комнаты.");
          return;
        }

        var roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent
        {
          Room = room,
          Users = room.Users.Select(nick => server.Users[nick]).ToList()
        };
        ServerModel.Server.SendMessage(args.ConnectionId, ClientRoomRefreshedCommand.CommandId, roomRefreshedContent);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private string roomName;

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }
    }
  }
}
