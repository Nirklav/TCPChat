using Engine.Model.Client;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security;
using Engine.Exceptions;

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
      {
        Room prevRoom;
        if (!client.Rooms.TryGetValue(content.Room.Name, out prevRoom))
          throw new ModelException(ErrorCode.RoomNotFound);

        client.Rooms[content.Room.Name] = content.Room;
        content.Room.Enabled = prevRoom.Enabled;

        UpdateUsers(client, content.Users);
        UpdateRoomUsers(client, content.Room, prevRoom);
        UpdateRoomFiles(client, content.Room, prevRoom);
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

    [SecurityCritical]
    private static void UpdateRoomUsers(ClientContext client, Room currentRoom, Room prevRoom)
    {
      var removed = new HashSet<string>(prevRoom.Users);
      var added = new HashSet<string>(currentRoom.Users);

      foreach (var nick in prevRoom.Users)
        added.Remove(nick);
      foreach (var nick in currentRoom.Users)
        removed.Remove(nick);

      if (currentRoom.Name == ServerModel.MainRoomName)
      {
        foreach (var nick in removed)
          client.Users.Remove(nick);
      }

      // TODO: Maybe use OOP. But room is common entity for server and client. 
      // And this is only client operation.
      // So it would be strange to add it to the common entity.
      if (currentRoom.Enabled && currentRoom.Type == RoomType.Voice)
      {
        foreach (var nick in added)
          ClientModel.Api.AddInterlocutor(nick);
        foreach (var nick in removed)
          ClientModel.Api.RemoveInterlocutor(nick);
      }
    }

    [SecurityCritical]
    private static void UpdateRoomFiles(ClientContext client, Room currentRoom, Room prevRoom)
    {
      var removed = new HashSet<FileId>(prevRoom.Files.Select(f => f.Id));
      foreach (var file in currentRoom.Files)
        removed.Remove(file.Id);

      foreach (var fileId in removed)
        ClientModel.Api.ClosePostedFile(client, currentRoom.Name, fileId);
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
