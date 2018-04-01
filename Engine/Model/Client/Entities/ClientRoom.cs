using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Model.Client.Entities
{
  [Serializable]
  public class ClientRoom : Room
  {
    [SecuritySafeCritical]
    public ClientRoom(RoomDto dto)
      : base(dto.Admin, dto.Name)
    {
      foreach (var nick in dto.Users)
        _users.Add(nick);

      foreach (var file in dto.Files)
        _files.Add(file.Id, new FileDescription(file));

      foreach (var message in dto.Messages)
        _messages.Add(message.Id, new Message(message));
    }

    #region files
    [SecuritySafeCritical]
    public override bool RemoveFile(FileId fileId)
    {
      var result = base.RemoveFile(fileId);
      if (result)
      {
        var chat = ClientGuard.CurrentChat;

        // Remove downloading
        if (chat.IsFileDownloading(fileId))
          chat.RemoveFileDownload(fileId);

        // Notify
        var downloadEventArgs = new FileDownloadEventArgs(Name, fileId, 0);
        ClientModel.Notifier.PostedFileDeleted(downloadEventArgs);
      }
      return result;
    }
    #endregion

    #region dispose
    protected override void ReleaseManagedResources()
    {
      base.ReleaseManagedResources();

      // Remove all posted files.
      var chat = ClientGuard.CurrentChat;
      foreach (var file in Files)
      {
        if (file.Id.Owner == chat.User.Id)
          chat.RemovePostedFile(Name, file.Id);
      }
    }
    #endregion
  }
}
