using Engine.Api.Client;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Security;

namespace Engine.Api.Server
{
  [SecurityCritical]
  class ServerKickUsersCommand :
    ServerCommand<ServerKickUsersCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.KickUsers;

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

      if (content.Users == null)
        throw new ArgumentNullException("Users");

      if (string.Equals(content.RoomName, ServerModel.MainRoomName))
      {
        ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAccessDenied);
        return;
      }

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server, content.RoomName, args.ConnectionId, out room))
          return;

        if (!room.Admin.Equals(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAccessDenied);
          return;
        }

        var sendingContent = new ClientRoomClosedCommand.MessageContent { Room = room };

        foreach (var userNick in content.Users)
        {
          if (!room.ContainsUser(userNick))
            continue;

          if (userNick.Equals(room.Admin))
          {
            ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAccessDenied);
            continue;
          }

          room.RemoveUser(userNick);

          ServerModel.Server.SendMessage(userNick, ClientRoomClosedCommand.CommandId, sendingContent);
        }

        RefreshRoom(server, room);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private string _roomName;
      private List<string> _users;

      public string RoomName
      {
        get { return _roomName; }
        set { _roomName = value; }
      }

      public List<string> Users
      {
        get { return _users; }
        set { _users = value; }
      }
    }
  }
}
