using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Collections.Generic;
using Engine.Helpers;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientRoomRefreshedCommand :
    ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.RoomRefreshed;

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

      using (var client = ClientModel.Get())
        client.Rooms[receivedContent.Room.Name] = receivedContent.Room;

      ClientModel.Notifier.RoomRefreshed(new RoomEventArgs { Room = receivedContent.Room, Users = receivedContent.Users });
    }

    [Serializable]
    public class MessageContent
    {
      private Room room;
      private List<User> users;
 
      public Room Room
      {
        get { return room; }
        set { room = value; }
      }

      public List<User> Users
      {
        get { return users; }
        set { users = value; }
      }
    }
  }
}
