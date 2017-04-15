using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Drawing;
using System.Security;

namespace Engine.Model.Client.Entities
{
  [Serializable]
  public class ClientUser : User
  {
    private int _voiceCounter;

    /// <summary>
    /// Creates new instance of user.
    /// </summary>
    /// <param name="nick">User nick.</param>
    /// <param name="nickColor">Nick color.</param>
    [SecuritySafeCritical]
    public ClientUser(string nick, Color color)
      : base(nick, color)
    {

    }

    /// <summary>
    /// Creates new instance of user.
    /// </summary>
    /// <param name="dto">Data transfer object of user.</param>
    [SecuritySafeCritical]
    public ClientUser(UserDto dto)
      : base(dto)
    {

    }

    /// <summary>
    /// Checks is voice active for user.
    /// </summary>
    /// <returns>Returns true if voice active, otherwise false.</returns>
    [SecuritySafeCritical]
    public bool IsVoiceActive()
    {
      return _voiceCounter > 0;
    }

    /// <summary>
    /// Increments voice counter.
    /// Counter used to check voice state (enabled/disabled). 
    /// </summary>
    [SecuritySafeCritical]
    public void IncVoiceCounter()
    {
      _voiceCounter++;
    }

    /// <summary>
    /// Decrements voice counter.
    /// Counter used to check voice state (enabled/disabled). 
    /// </summary>
    [SecuritySafeCritical]
    public void DecVoiceCounter()
    {
      if (_voiceCounter == 0)
        throw new InvalidOperationException("Can't decrement voice counter.");
      _voiceCounter--;
    }
  }
}
