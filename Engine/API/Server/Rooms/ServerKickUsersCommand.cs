using Engine.Api.Client;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
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
        throw new ArgumentException("content.RoomName");

      if (content.Users == null)
        throw new ArgumentNullException("content.Users");

      if (content.RoomName == ServerChat.MainRoomName)
      {
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
        return;
      }

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server.Chat, content.RoomName, args.ConnectionId, out room))
          return;

        if (room.Admin != args.ConnectionId)
        {
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
          return;
        }

        var sendingContent = new ClientRoomClosedCommand.MessageContent { RoomName = room.Name };

        foreach (var userNick in content.Users)
        {
          if (!room.IsUserExist(userNick))
            continue;

          if (userNick == room.Admin)
          {
            ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
            continue;
          }

          room.RemoveUser(userNick);
          ServerModel.Server.SendMessage(userNick, ClientRoomClosedCommand.CommandId, sendingContent);
        }

        RefreshRoom(server.Chat, room);
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
