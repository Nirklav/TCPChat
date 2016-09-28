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
    /// <param name="nick">User nick.</param>
    [SecuritySafeCritical]
    public override void AddUser(string nick)
    {
      base.AddUser(nick);

      if (Enabled)
        IncVoiceCoutner(nick);
    }

    /// <summary>
    /// Remove user from room, including all his files.
    /// </summary>
    /// <param name="nick">User nick.</param>
    [SecuritySafeCritical]
    public override void RemoveUser(string nick)
    {
      base.RemoveUser(nick);

      if (Enabled)
        DecVoiceCoutner(nick);
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
        foreach (var nick in _users)
          IncVoiceCoutner(nick);
      }

      base.Enable();
    }

    /// <summary>
    /// Disable room.
    /// </summary>
    [SecuritySafeCritical]
    public override void Disable()
    {
      if (!Enabled)
      {
        foreach (var nick in _users)
          DecVoiceCoutner(nick);
      }

      base.Disable();
    }

    [SecuritySafeCritical]
    private void IncVoiceCoutner(string nick)
    {
      // User can be already removed from chat.
      // e.g.
      //  1) We receive RoomRefreshed on MainRoom and remove user form chat.
      //  2) We receive RoomRefresged on other audio room.

      var user = ClientGuard.CurrentChat.TryGetUser(nick);
      if (user != null)
        user.IncVoiceCounter();
    }

    [SecuritySafeCritical]
    private void DecVoiceCoutner(string nick)
    {
      // See comment above.
      var user = ClientGuard.CurrentChat.TryGetUser(nick);
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
        if (file.Id.Owner == chat.User.Nick)
          chat.RemovePostedFile(Name, file.Id);
      }
    }
    #endregion
  }
}
