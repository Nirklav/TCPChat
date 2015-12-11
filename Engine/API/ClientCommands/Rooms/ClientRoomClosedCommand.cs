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
    public override void Run(MessageContent content, ClientCommandArgs args)
    {
      if (content.Room == null)
        throw new ArgumentNullException("room");

      ClientModel.Notifier.RoomClosed(new RoomEventArgs { Room = content.Room });

      using (var client = ClientModel.Get())
        client.Rooms.Remove(content.Room.Name);
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
