using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Collections.Generic;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientRoomOpenedCommand :
      IClientCommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (receivedContent.Room == null)
        throw new ArgumentNullException("room");

      if (receivedContent.Type == RoomType.Voice)
      {
        VoiceRoom room = receivedContent.Room as VoiceRoom;

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

      ClientModel.OnRoomOpened(this, new RoomEventArgs { Room = receivedContent.Room, Users = receivedContent.Users });
    }

    [Serializable]
    public class MessageContent
    {
      Room room;
      RoomType type;
      List<User> users;

      public Room Room { get { return room; } set { room = value; } }
      public RoomType Type { get { return type; } set { type = value; } }
      public List<User> Users { get { return users; } set { users = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.RoomOpened;
  }
}
