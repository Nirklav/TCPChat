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
    public static readonly UserId Empty = new UserId { _nick = null };

    [BinField("i")]
    private string _nick;

    [SecuritySafeCritical]
    public UserId(string nick)
    {
      _nick = nick ?? throw new ArgumentNullException(nameof(nick));
    }

    [SecuritySafeCritical]
    public UserId(long tempId)
    {
      _nick = string.Format("{0}{1}", TempConnectionPrefix, tempId);
    }

    /// <summary>
    /// User nickname.
    /// </summary>
    public string Nick { get { return _nick; } }

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
      return string.Equals(Nick, other.Nick, StringComparison.Ordinal);
    }

    [SecuritySafeCritical]
    public override int GetHashCode()
    {
      return _nick == null ? 0 : _nick.GetHashCode();
    }

    [SecuritySafeCritical]
    public override string ToString()
    {
      return _nick;
    }
  }
}
