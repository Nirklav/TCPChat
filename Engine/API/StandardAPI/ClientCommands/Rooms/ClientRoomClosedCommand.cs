using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientRoomClosedCommand :
      ICommand<ClientCommandArgs>
  {
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
      Room room;

      public Room Room { get { return room; } set { room = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.RoomClosed;
  }
}
