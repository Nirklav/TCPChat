using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Model.Client.Entities
{
  [Serializable]
  public class ClientVoiceRoom : VoiceRoom
  {
    [SecuritySafeCritical]
    public ClientVoiceRoom(RoomDto dto)
      : base(dto.Admin, dto.Name)
    {
      foreach (var nick in dto.Users)
        _users.Add(nick);

      foreach (var file in dto.Files)
        _files.Add(file.Id, new FileDescription(file));

      foreach (var message in dto.Messages)
        _messages.Add(message.Id, new Message(message));
    }

    #region users
    /// <summary>
    /// Add user to room.
    /// </summary>
    /// <param name="userId">User id.</param>
    [SecuritySafeCritical]
    public override void AddUser(UserId userId)
    {
      base.AddUser(userId);

      if (Enabled)
        IncVoiceCoutner(userId);
    }

    /// <summary>
    /// Remove user from room, including all his files.
    /// </summary>
    /// <param name="userId">User id.</param>
    [SecuritySafeCritical]
    public override void RemoveUser(UserId userId)
    {
      base.RemoveUser(userId);

      if (Enabled)
        DecVoiceCoutner(userId);
    }
    #endregion

    #region enable/disable
    /// <summary>
    /// Enable room.
    /// </summary>
    [SecuritySafeCritical]
    public override void Enable()
    {
      if (!Enabled)
      {
        foreach (var userId in _users)
          IncVoiceCoutner(userId);
      }

      base.Enable();
    }

    /// <summary>
    /// Disable room.
    /// </summary>
    [SecuritySafeCritical]
    public override void Disable()
    {
      if (Enabled)
      {
        foreach (var userId in _users)
          DecVoiceCoutner(userId);
      }

      base.Disable();
    }

    [SecuritySafeCritical]
    private void IncVoiceCoutner(UserId userId)
    {
      // User can be already removed from chat.
      // e.g.
      //  1) We receive RoomRefreshed on MainRoom and remove user form chat.
      //  2) We receive RoomRefresged on other audio room.

      var user = ClientGuard.CurrentChat.TryGetUser(userId);
      if (user != null)
        user.IncVoiceCounter();
    }

    [SecuritySafeCritical]
    private void DecVoiceCoutner(UserId userId)
    {
      // See comment above.
      var user = ClientGuard.CurrentChat.TryGetUser(userId);
      if (user != null)
        user.DecVoiceCounter();
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
