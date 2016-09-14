using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using System;
using System.Security;

namespace Engine.Api.Server
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
        throw new ArgumentException("content.RoomName");

      if (string.IsNullOrEmpty(content.NewAdmin))
        throw new ArgumentNullException("content.NewAdmin");

      if (content.RoomName == ServerChat.MainRoomName)
      {
        ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAccessDenied);
        return;
      }

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server.Chat, content.RoomName, args.ConnectionId, out room))
          return;

        if (room.Admin != args.ConnectionId)
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAccessDenied);
          return;
        }

        if (!room.IsUserExist(content.NewAdmin))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomUserNotExist);
          return;
        }

        room.Admin = content.NewAdmin;
        ServerModel.Api.SendSystemMessage(content.NewAdmin, SystemMessageId.RoomAdminChanged, room.Name);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private string _roomName;
      private string _newAdmin;

      public string RoomName
      {
        get { return _roomName; }
        set { _roomName = value; }
      }

      public string NewAdmin
      {
        get { return _newAdmin; }
        set { _newAdmin = value; }
      }
    }
  }
}
