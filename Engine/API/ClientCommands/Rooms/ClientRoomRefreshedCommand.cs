using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Collections.Generic;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientRoomRefreshedCommand :
    ClientCommand<ClientRoomRefreshedCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.RoomRefreshed;

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
        client.Rooms[content.Room.Name] = content.Room;

      ClientModel.Notifier.RoomRefreshed(new RoomEventArgs { Room = content.Room, Users = content.Users });
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
