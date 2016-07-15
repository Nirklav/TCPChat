using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security;

namespace Engine.API.ClientCommands
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

      if (content.Type == RoomType.Voice)
      {
        var room = content.Room as VoiceRoom;
        if (room == null)
          throw new ArgumentException("type");

        string userNick;
        List<string> mapForUser;

        using (var client = ClientModel.Get())
        {
          userNick = client.User.Nick;
          mapForUser = room.ConnectionMap[userNick];
        }

        foreach (var nick in room.Users)
        {
          if (nick.Equals(userNick))
            continue;

          ClientModel.Api.AddInterlocutor(nick);
        }

        foreach (string nick in mapForUser)
          ClientModel.Api.ConnectToPeer(nick);
      }

      using (var client = ClientModel.Get())
      {
        client.Rooms.Add(content.Room.Name, content.Room);
        UpdateUsers(client, content.Users);
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
      private Room room;
      private RoomType type;
      private List<User> users;

      public Room Room
      {
        get { return room; }
        set { room = value; }
      }

      public RoomType Type
      {
        get { return type; }
        set { type = value; }
      }

      public List<User> Users
      {
        get { return users; }
        set { users = value; }
      }
    }
  }
}
