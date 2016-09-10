using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
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
        content.Room.Enabled = true;
        client.Rooms.Add(content.Room.Name, content.Room);

        UpdateUsers(client, content.Users);

        if (content.Type == RoomType.Voice)
        {
          var room = content.Room as VoiceRoom;
          if (room == null)
            throw new ArgumentException("type");

          foreach (var nick in room.Users)
          {
            if (nick == client.User.Nick)
              continue;

            ClientModel.Api.AddInterlocutor(nick);
          }

          var mapForUser = room.ConnectionMap[client.User.Nick];
          foreach (var nick in mapForUser)
            ClientModel.Api.ConnectToPeer(nick);
        }
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
      private Room _room;
      private RoomType _type;
      private List<User> _users;

      public Room Room
      {
        get { return _room; }
        set { _room = value; }
      }

      public RoomType Type
      {
        get { return _type; }
        set { _type = value; }
      }

      public List<User> Users
      {
        get { return _users; }
        set { _users = value; }
      }
    }
  }
}
