using System;
using System.Security;
using Engine.Api.Client.P2P;
using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Client.Entities;
using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.Rooms
{
  [SecurityCritical]
  class ClientRoomOpenedCommand :
    ClientCommand<ClientRoomOpenedCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.RoomOpened;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (content.Room == null)
        throw new ArgumentNullException("content.Room");

      using (var client = ClientModel.Get())
      {
        Room room = null;
        if (content.Room.Type == RoomType.Chat)
        {
          var textRoom = new ClientRoom(content.Room);
          client.Chat.AddRoom(textRoom);
          room = textRoom;
        }

        if (content.Room.Type == RoomType.Voice)
        {
          var voiceRoom = new ClientVoiceRoom(content.Room);
          client.Chat.AddVoiceRoom(voiceRoom);
          room = voiceRoom;
        }

        if (room == null)
          throw new ModelException(ErrorCode.UnknownRoomType);

        AddUsers(client.Chat, content.Users);

        // Create P2P connections to users if need
        if (content.Room.ConnectTo != null)
        {
          foreach (var nick in content.Room.ConnectTo)
            ClientModel.Api.Perform(new ClientConnectToPeerAction(nick));
        }

        room.Enable();
      }
      
      ClientModel.Notifier.RoomOpened(new RoomOpenedEventArgs(content.Room.Name));
    }

    [Serializable]
    [BinType("ClientRoomOpened")]
    public class MessageContent
    {
      [BinField("r")]
      public RoomDto Room;

      [BinField("u")]
      public UserDto[] Users;
    }
  }
}
