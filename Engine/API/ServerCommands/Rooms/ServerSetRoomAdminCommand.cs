using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerSetRoomAdminCommand :
    ServerCommand<ServerSetRoomAdminCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.SetRoomAdmin;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ServerCommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("RoomName");

      if (content.NewAdmin == null)
        throw new ArgumentNullException("NewAdmin");

      if (string.Equals(content.RoomName, ServerModel.MainRoomName))
      {
        ServerModel.Api.SendSystemMessage(args.ConnectionId, "Невозможно назначить администратора для главной комнаты.");
        return;
      }

      if (!RoomExists(content.RoomName, args.ConnectionId))
        return;

      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[content.RoomName];

        if (!room.Admin.Equals(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, "Вы не являетесь администратором комнаты. Операция отменена.");
          return;
        }

        room.Admin = content.NewAdmin.Nick;

        var message = string.Format("Вы назначены администратором комнаты \"{0}\".", room.Name);
        ServerModel.Api.SendSystemMessage(content.NewAdmin.Nick, message);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private string roomName;
      private User newAdmin;

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }

      public User NewAdmin
      {
        get { return newAdmin; }
        set { newAdmin = value; }
      }
    }
  }
}
