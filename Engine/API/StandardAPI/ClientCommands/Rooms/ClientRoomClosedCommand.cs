using Engine.Model.Client;
using Engine.Model.Entities;
using System;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientRoomClosedCommand :
      BaseCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFromMessage<MessageContent>(args.Message);

      if (receivedContent.Room == null)
        throw new ArgumentNullException("room");

      ClientModel.OnRoomClosed(this, new RoomEventArgs { Room = receivedContent.Room });

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
