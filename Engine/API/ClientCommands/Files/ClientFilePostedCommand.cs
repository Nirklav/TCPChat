using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Security;

namespace Engine.API.ClientCommands
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
        Message = content.File.Name,
        Sender = content.File.Owner.Nick,
        RoomName = content.RoomName,
        FileId = content.File.Id,
      };

      ClientModel.Notifier.ReceiveMessage(receiveMessageArgs);
    }

    [Serializable]
    public class MessageContent
    {
      private FileDescription file;
      private string roomName;

      public FileDescription File
      {
        get { return file; }
        set { file = value; }
      }

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }
    }
  }
}
