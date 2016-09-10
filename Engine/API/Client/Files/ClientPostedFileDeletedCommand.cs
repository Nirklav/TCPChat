using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Api.Client
{
  [SecurityCritical]
  class ClientPostedFileDeletedCommand :
    ClientCommand<ClientPostedFileDeletedCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.PostedFileDeleted;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("roomName");

      using (var client = ClientModel.Get())
        client.Chat.RemovePostedFile(content.RoomName, content.FileId);
    }

    [Serializable]
    public class MessageContent
    {
      private string _roomName;
      private FileId _fileId;

      public string RoomName
      {
        get { return _roomName; }
        set { _roomName = value; }
      }

      public FileId FileId
      {
        get { return _fileId; }
        set { _fileId = value; }
      }
    }
  }
}
