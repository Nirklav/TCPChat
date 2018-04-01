using System;
using System.Security;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Model.Common.Entities
{
  [Serializable]
  [BinType("UserIdDto")]
  public struct UserId : IEquatable<UserId>
  {
    private const string TempConnectionPrefix = "tempId_";
    public static readonly UserId Empty = new UserId();

    [BinField("i")]
    private string _nick;
    [BinField("c")]
    private string _thumbprint;

    [SecuritySafeCritical]
    public UserId(string nick, string thumbprint)
    {
      if (string.IsNullOrWhiteSpace(nick))
        throw new ArgumentException(nameof(nick));
      if (string.IsNullOrWhiteSpace(thumbprint))
        throw new ArgumentException(nameof(thumbprint));

      _nick = nick;
      _thumbprint = thumbprint;
    }

    [SecuritySafeCritical]
    public UserId(long tempId, string thumbprint)
    {
      if (string.IsNullOrWhiteSpace(thumbprint))
        throw new ArgumentException(nameof(thumbprint));

      _nick = string.Format("{0}{1}", TempConnectionPrefix, tempId);
      _thumbprint = thumbprint;
    }

    /// <summary>
    /// User nickname.
    /// </summary>
    public string Nick { get { return _nick; } }

    /// <summary>
    /// User certificate thumbprint.
    /// </summary>
    public string Thumbprint { get { return _thumbprint; } }

    /// <summary>
    /// Returns true if user id is temporary.
    /// </summary>
    public bool IsTemporary { get { return Nick != null && Nick.StartsWith(TempConnectionPrefix); } }

    [SecuritySafeCritical]
    public static bool operator ==(UserId first, UserId second)
    {
      return first.Equals(second);
    }

    [SecuritySafeCritical]
    public static bool operator !=(UserId first, UserId second)
    {
      return !first.Equals(second);
    }

    [SecuritySafeCritical]
    public override bool Equals(object obj)
    {
      if (obj == null)
        return false;
      if (!(obj is UserId))
        return false;
      return Equals((UserId)obj);
    }

    [SecuritySafeCritical]
    public bool Equals(UserId other)
    {
      return string.Equals(_nick, other._nick, StringComparison.Ordinal)
        && string.Equals(_thumbprint, other._thumbprint, StringComparison.CurrentCultureIgnoreCase);
    }

    [SecuritySafeCritical]
    public override int GetHashCode()
    {
      unchecked
      {
        var hash = _nick == null ? 0 : _nick.GetHashCode();
        hash = (hash * 397) ^ (_thumbprint == null ? 0 : _thumbprint.GetHashCode());
        return hash;
      }
    }

    [SecuritySafeCritical]
    public override string ToString()
    {
      return string.Format("{0}:{1}", _nick, _thumbprint);
    }
  }
}
