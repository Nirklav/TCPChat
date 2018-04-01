using System;
using System.Collections.Generic;
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
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException(nameof(content.RoomName));

      if (content.Users == null)
        throw new ArgumentNullException(nameof(content.Users));

      if (content.RoomName == ServerChat.MainRoomName)
      {
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomItsMainRoom));
        return;
      }

      using (var server = ServerModel.Get())
      {
        if (!TryGetRoom(server.Chat, content.RoomName, args.ConnectionId, out Room room))
          return;

        if (!room.Admin.Equals(args.ConnectionId))
        {
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
          return;
        }

        var invitedUsers = new HashSet<UserId>();
        foreach (var userId in content.Users)
        {
          if (room.IsUserExist(userId))
            continue;

          room.AddUser(userId);
          invitedUsers.Add(userId);
        }

        var users = server.Chat.GetRoomUserDtos(room.Name);
        foreach (var userId in room.Users)
        {
          var roomDto = room.ToDto(userId);

          if (invitedUsers.Contains(userId))
          {
            var roomOpenContent = new ClientRoomOpenedCommand.MessageContent
            {
              Room = roomDto,
              Users = users
            };

            ServerModel.Server.SendMessage(userId, ClientRoomOpenedCommand.CommandId, roomOpenContent);
          }
          else
          {
            var roomRefreshContent = new ClientRoomRefreshedCommand.MessageContent
            {
              Room = roomDto,
              Users = users
            };

            ServerModel.Server.SendMessage(userId, ClientRoomRefreshedCommand.CommandId, roomRefreshContent);
          }
        }
      }
    }

    [Serializable]
    [BinType("ServerInviteUsers")]
    public class MessageContent
    {
      [BinField("r")]
      public string RoomName;

      [BinField("u")]
      public UserId[] Users;
    }
  }
}
