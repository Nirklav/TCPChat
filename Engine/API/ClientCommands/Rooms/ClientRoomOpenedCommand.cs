using Engine.Model.Client;
using Engine.Model.Entities;
using System;
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
    public override void Run(MessageContent content, ClientCommandArgs args)
    {
      if (content.Room == null)
        throw new ArgumentNullException("room");

      if (content.Type == RoomType.Voice)
      {
        var room = content.Room as VoiceRoom;
        if (room == null)
          throw new ArgumentException("type");

        List<string> mapForUser;

        using (var client = ClientModel.Get())
          mapForUser = room.ConnectionMap[client.User.Nick];

        foreach (string nick in mapForUser)
          ClientModel.Api.ConnectToPeer(nick);
      }

      using (var client = ClientModel.Get())
        client.Rooms.Add(content.Room.Name, content.Room);

      ClientModel.Notifier.RoomOpened(new RoomEventArgs { Room = content.Room, Users = content.Users });
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
