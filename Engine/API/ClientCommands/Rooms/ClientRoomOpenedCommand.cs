using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Collections.Generic;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientRoomOpenedCommand :
    ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.RoomOpened;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ClientCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);
      if (receivedContent.Room == null)
        throw new ArgumentNullException("room");

      if (receivedContent.Type == RoomType.Voice)
      {
        var room = receivedContent.Room as VoiceRoom;
        if (room == null)
          throw new ArgumentException("type");

        List<string> mapForUser;

        using (var client = ClientModel.Get())
          mapForUser = room.ConnectionMap[client.User.Nick];

        foreach (string nick in mapForUser)
          ClientModel.API.ConnectToPeer(nick);
      }

      using (var client = ClientModel.Get())
        client.Rooms.Add(receivedContent.Room.Name, receivedContent.Room);

      ClientModel.Notifier.RoomOpened(new RoomEventArgs { Room = receivedContent.Room, Users = receivedContent.Users });
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
