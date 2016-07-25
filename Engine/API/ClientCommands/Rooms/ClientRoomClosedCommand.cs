using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientRoomClosedCommand :
    ClientCommand<ClientRoomClosedCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.RoomClosed;

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

      ClientModel.Notifier.RoomClosed(new RoomEventArgs { RoomName = content.Room.Name });

      using (var client = ClientModel.Get())
      {
        var room = client.Rooms[content.Room.Name];
        client.Rooms.Remove(content.Room.Name);

        if (room.Enabled && room.Type == RoomType.Voice)
        {
          foreach (var nick in room.Users)
          {
            if (nick == client.User.Nick)
              continue;

            ClientModel.Api.RemoveInterlocutor(nick);
          }
        }
      }
    }

    [Serializable]
    public class MessageContent
    {
      private Room room;

      public Room Room
      {
        get { return room; }
        set { room = value; }
      }
    }
  }
}
