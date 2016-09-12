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
        _files.Add(file.Id, file);

      foreach (var message in dto.Messages)
        _messages.Add(message.Id, message);

      foreach (var kvp in dto.ConnectionsMap)
      {
        var nick = kvp.Key;
        var connectTo = kvp.Value;
        _connectionMap.Add(nick, connectTo);
      }
    }

    #region users
    /// <summary>
    /// Add user to room.
    /// </summary>
    /// <param name="nick">User nick.</param>
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
    public override void Disable()
    {
      if (!Enabled)
      {
        foreach (var nick in _users)
          DecVoiceCoutner(nick);
      }

      base.Disable();
    }


    private void IncVoiceCoutner(string nick)
    {
      var user = ClientGuard.Current.Chat.GetUser(nick);
      user.IncVoiceCounter();
    }

    private void DecVoiceCoutner(string nick)
    {
      var user = ClientGuard.Current.Chat.GetUser(nick);
      user.DecVoiceCounter();
    }
    #endregion
  }
}
