using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientRoomClosedCommand :
    ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.RoomClosed;

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

      ClientModel.Notifier.RoomClosed(new RoomEventArgs { Room = receivedContent.Room });

      using (var client = ClientModel.Get())
        client.Rooms.Remove(receivedContent.Room.Name);
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
