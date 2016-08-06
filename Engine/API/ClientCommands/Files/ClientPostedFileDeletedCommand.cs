using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Security;

namespace Engine.API.ClientCommands
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
        ClientModel.Api.ClosePostedFile(client, content.RoomName, content.FileId);
    }

    [Serializable]
    public class MessageContent
    {
      private string roomName;
      private FileId fileId;

      public string RoomName
      {
        get { return roomName; }
        set { roomName = value; }
      }

      public FileId FileId
      {
        get { return fileId; }
        set { fileId = value; }
      }
    }
  }
}
