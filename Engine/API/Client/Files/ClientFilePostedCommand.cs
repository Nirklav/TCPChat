using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Api.Client
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
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (content.File == null)
        throw new ArgumentNullException("content.File");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("content.RoomName");

      using (var client = ClientModel.Get())
      {
        var room = client.Chat.GetRoom(content.RoomName);
        room.AddFile(content.File);
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
    public class MessageContent
    {
      private FileDescription _file;
      private string _roomName;

      public FileDescription File
      {
        get { return _file; }
        set { _file = value; }
      }

      public string RoomName
      {
        get { return _roomName; }
        set { _roomName = value; }
      }
    }
  }
}
