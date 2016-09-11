using Engine.Api.Client.P2P;
using Engine.Model.Client;
using Engine.Model.Client.Entities;
using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Api.Client
{
  [SecurityCritical]
  class ClientRoomOpenedCommand :
    ClientCommand<ClientRoomOpenedCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.RoomOpened;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (content.Room == null)
        throw new ArgumentNullException("room");

      using (var client = ClientModel.Get())
      {
        Room room = null;
        if (content.Room.Type == RoomType.Chat)
        {
          var textRoom = new ClientRoom(content.Room);
          client.Chat.AddRoom(textRoom);
          room = textRoom;
        }

        if (content.Room.Type == RoomType.Voice)
        {
          var voiceRoom = new ClientVoiceRoom(content.Room);
          client.Chat.AddVoiceRoom(voiceRoom);

          var mapForUser = voiceRoom.ConnectionMap[client.Chat.User.Nick];
          foreach (var nick in mapForUser)
            ClientModel.Api.Perform(new ClientConnectToPeerAction(nick));

          room = voiceRoom;
        }

        AddUsers(client.Chat, content.Users);
        room.Enable();
      }

      var eventArgs = new RoomEventArgs
      {
        RoomName = content.Room.Name,
        Users = content.Users
          .Select(u => u.Nick)
          .ToList()
      };
      ClientModel.Notifier.RoomOpened(eventArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private RoomDto _room;
      private List<UserDto> _users;

      public RoomDto Room
      {
        get { return _room; }
        set { _room = value; }
      }

      public List<UserDto> Users
      {
        get { return _users; }
        set { _users = value; }
      }
    }
  }
}
