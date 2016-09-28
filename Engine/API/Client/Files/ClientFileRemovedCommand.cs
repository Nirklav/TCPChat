using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Security;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client
{
  [SecurityCritical]
  class ClientFileRemovedCommand :
    ClientCommand<ClientFileRemovedCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.FileRemoved;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("content.RoomName");

      using (var client = ClientModel.Get())
      {
        // Remove file from room
        var room = client.Chat.TryGetRoom(content.RoomName);
        if (room != null)
          room.RemoveFile(content.FileId);
      }
    }

    [Serializable]
    [BinType("ClientFileRemoved")]
    public class MessageContent
    {
      [BinField("r")]
      public string RoomName;

      [BinField("f")]
      public FileId FileId;
    }
  }
}
