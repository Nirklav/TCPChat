using Engine.Model.Client;
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
      if (content.RoomName == null)
        throw new ArgumentNullException("content.RoomName");

      using (var client = ClientModel.Get())
        client.Chat.RemoveRoom(content.RoomName);

      ClientModel.Notifier.RoomClosed(new RoomEventArgs(content.RoomName));
    }

    [Serializable]
    public class MessageContent
    {
      private string _roomName;

      public string RoomName
      {
        get { return _roomName; }
        set { _roomName = value; }
      }
    }
  }
}
