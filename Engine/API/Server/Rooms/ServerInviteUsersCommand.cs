using Engine.Api.Client;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using System;
using System.Collections.Generic;
using System.Security;

namespace Engine.Api.Server
{
  [SecurityCritical]
  class ServerInviteUsersCommand :
    ServerCommand<ServerInviteUsersCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.InvateUsers;

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

      if (content.Users == null)
        throw new ArgumentNullException("content.Users");

      if (content.RoomName == ServerChat.MainRoomName)
      {
        ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomItsMainRoom);
        return;
      }

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server.Chat, content.RoomName, args.ConnectionId, out room))
          return;

        if (!room.Admin.Equals(args.ConnectionId))
        {
          ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.RoomAccessDenied);
          return;
        }

        var invitedUsers = new HashSet<string>();
        foreach (var userNick in content.Users)
        {
          if (room.IsUserExist(userNick))
            continue;

          room.AddUser(userNick);
          invitedUsers.Add(userNick);
        }

        var users = server.Chat.GetRoomUserDtos(room.Name);

        foreach (var userNick in room.Users)
        {
          var roomDto = room.ToDto(userNick);

          if (invitedUsers.Contains(userNick))
          {
            var roomOpenContent = new ClientRoomOpenedCommand.MessageContent
            {
              Room = roomDto,
              Users = users
            };

            ServerModel.Server.SendMessage(userNick, ClientRoomOpenedCommand.CommandId, roomOpenContent);
          }
          else
          {
            var roomRefreshContent = new ClientRoomRefreshedCommand.MessageContent
            {
              Room = roomDto,
              Users = users
            };

            ServerModel.Server.SendMessage(userNick, ClientRoomRefreshedCommand.CommandId, roomRefreshContent);
          }
        }
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
