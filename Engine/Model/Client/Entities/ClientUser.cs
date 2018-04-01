using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Drawing;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace Engine.Model.Client.Entities
{
  [Serializable]
  public class ClientUser : User
  {
    private int _voiceCounter;
    private X509Certificate2 _certificate;

    /// <summary>
    /// Creates new instance of user.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <param name="nickColor">Nick color.</param>
    /// <param name="certificate">User certificate.</param>
    [SecuritySafeCritical]
    public ClientUser(UserId id, Color color, X509Certificate2 certificate)
      : base(id, color)
    {
      _certificate = certificate;
    }

    /// <summary>
    /// Creates new instance of user.
    /// </summary>
    /// <param name="dto">Data transfer object of user.</param>
    [SecuritySafeCritical]
    public ClientUser(UserDto dto)
      : base(dto)
    {
      _certificate = new X509Certificate2(dto.Certificate);
    }

    /// <summary>
    /// User certificate.
    /// </summary>
    public X509Certificate2 Certificate
    {
      [SecuritySafeCritical]
      get { return _certificate; }
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
