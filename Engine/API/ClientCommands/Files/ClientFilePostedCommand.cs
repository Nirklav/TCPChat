using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Security;

namespace Engine.Api.ClientCommands
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
        throw new ArgumentNullException("file");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("roomName");

      using (var client = ClientModel.Get())
      {
        Room room;
        if (!client.Rooms.TryGetValue(content.RoomName, out room))
          return;

        room.Files.RemoveAll(f => f.Id == content.File.Id);
        room.Files.Add(content.File);
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
