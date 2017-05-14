using Engine.Exceptions;
using Engine.Model.Client;
using System;
using System.Collections.Generic;
using System.Security;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.Messages
{
  public class ClientRemoveMessagesCommand :
    ClientCommand<ClientRemoveMessagesCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.RemoveMessages;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("room name is null or empty");

      using (var client = ClientModel.Get())
      {
        var room = client.Chat.TryGetRoom(content.RoomName);
        if (room == null)
          throw new ModelException(ErrorCode.RoomNotFound);

        room.RemoveMessages(content.Ids);
      }

      var removed = new HashSet<long>(content.Ids);
      ClientModel.Notifier.RoomRefreshed(new RoomRefreshedEventArgs(content.RoomName, null, removed));
    }

    [Serializable]
    [BinType("ClientRemoveMessages")]
    public class MessageContent
    {
      [BinField("r")]
      public string RoomName;

      [BinField("i")]
      public long[] Ids;
    }
  }
}
