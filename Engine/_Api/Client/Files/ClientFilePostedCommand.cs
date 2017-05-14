using System;
using System.Security;
using Engine.Model.Client;
using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.Files
{
  [SecurityCritical]
  class ClientFilePostedCommand :
    ClientCommand<ClientFilePostedCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.FilePosted;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (content.File == null)
        throw new ArgumentNullException("content.File");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("content.RoomName");

      using (var client = ClientModel.Get())
      {
        var room = client.Chat.GetRoom(content.RoomName);

        // File may be posted twice, it's allowed
        if (!room.IsFileExist(content.File.Id))
          room.AddFile(new FileDescription(content.File));
      }

      var receiveMessageArgs = new ReceiveMessageEventArgs
      {
        Type = MessageType.File,
        MessageId = Room.SpecificMessageId,
        Time = DateTime.UtcNow,
        Message = content.File.Name,
        Sender = content.File.Id.Owner,
        RoomName = content.RoomName,
        FileId = content.File.Id,
      };

      ClientModel.Notifier.ReceiveMessage(receiveMessageArgs);
    }

    [Serializable]
    [BinType("ClientFilePosted")]
    public class MessageContent
    {
      [BinField("f")]
      public FileDescriptionDto File;

      [BinField("r")]
      public string RoomName;
    }
  }
}
