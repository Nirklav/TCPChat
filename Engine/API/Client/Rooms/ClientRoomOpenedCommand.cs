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
        throw new ArgumentNullException("content.Room");

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
          room = voiceRoom;
        }

        AddUsers(client.Chat, content.Users);

        // Create P2P connections to users if need
        if (content.Room.ConnectTo != null)
        {
          foreach (var nick in content.Room.ConnectTo)
            ClientModel.Api.Perform(new ClientConnectToPeerAction(nick));
        }

        room.Enable();
      }

      var users = content.Users
          .Select(u => u.Nick)
          .ToList();

      ClientModel.Notifier.RoomOpened(new RoomEventArgs(content.Room.Name, users));
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
