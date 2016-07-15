using Engine.Model.Client;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Linq;
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

      HashSet<string> added = null;
      HashSet<string> removed = null;

      using (var client = ClientModel.Get())
      {
        Room prevRoom;
        client.Rooms.TryGetValue(content.Room.Name, out prevRoom);
        client.Rooms[content.Room.Name] = content.Room;

        UpdateUsers(client, content.Users);

        added = new HashSet<string>(content.Room.Users);
        if (prevRoom != null)
        {
          foreach (var nick in prevRoom.Users)
            added.Remove(nick);

          removed = new HashSet<string>(prevRoom.Users);
          foreach (var nick in content.Room.Users)
            removed.Remove(nick);
        }

        if (removed != null && content.Room.Name == ServerModel.MainRoomName)
        {
          foreach (var nick in removed)
            client.Users.Remove(nick);
        }
      }

      // TODO: maybe use OOP, but room is common entity for server and client and this is only client operation
      if (content.Room.Type == RoomType.Voice)
      {
        if (added != null)
        {
          foreach (var nick in added)
            ClientModel.Api.AddInterlocutor(nick);
        }

        if (removed != null)
        {
          foreach (var nick in removed)
            ClientModel.Api.RemoveInterlocutor(nick);
        }
      }

      var eventArgs = new RoomEventArgs
      {
        RoomName = content.Room.Name,
        Users = content.Users
          .Select(u => u.Nick)
          .ToList()
      };
      ClientModel.Notifier.RoomRefreshed(eventArgs);
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
