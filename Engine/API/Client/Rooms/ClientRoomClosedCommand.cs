using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Security;

namespace Engine.Api.Client
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
        Room room;
        if (!client.Rooms.TryGetValue(content.Room.Name, out room))
          throw new ArgumentException("Room ");

        client.Rooms.Remove(content.Room.Name);
        client.PostedFiles.RemoveAll(f => room.Files.Exists(rf => rf.Id == f.File.Id));

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
      private Room _room;

      public Room Room
      {
        get { return _room; }
        set { _room = value; }
      }
    }
  }
}
