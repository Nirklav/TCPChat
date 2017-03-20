using System;
using System.Security;
using Engine.Model.Client;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.Rooms
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
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (content.RoomName == null)
        throw new ArgumentNullException("content.RoomName");

      using (var client = ClientModel.Get())
        client.Chat.RemoveRoom(content.RoomName);

      ClientModel.Notifier.RoomClosed(new RoomEventArgs(content.RoomName));
    }

    [Serializable]
    [BinType("ClientRoomClosed")]
    public class MessageContent
    {
      [BinField("r")]
      public string RoomName;
    }
  }
}
