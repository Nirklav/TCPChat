using Engine.Model.Common.Dto;
using System;
using System.Drawing;

namespace Engine.Model.Common.Entities
{
  /// <summary>
  /// User description.
  /// </summary>
  [Serializable]
  public class User : 
    IEquatable<User>,
    IEquatable<string>
  {
    private readonly string _nick;
    private readonly Color _nickColor;

    /// <summary>
    /// Creates new instance of user.
    /// </summary>
    /// <param name="nick">User nick.</param>
    /// <param name="nickColor">Nick color.</param>
    public User(string nick, Color nickColor)
    {
      _nick = nick;
      _nickColor = nickColor;
    }

    /// <summary>
    /// Creates new instance of user.
    /// </summary>
    /// <param name="dto">Data transfer object of user.</param>
    public User(UserDto dto)
    {
      _nick = dto.Nick;
      _nickColor = dto.NickColor;
    }

    /// <summary>
    /// User nick.
    /// </summary>
    public string Nick
    {
      get { return _nick; }
    }

    /// <summary>
    /// Nick color.
    /// </summary>
    public Color NickColor
    {
      get { return _nickColor; }
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(obj, null))
        return false;

      if (ReferenceEquals(obj, this))
        return true;

      var user = obj as User;
      if (ReferenceEquals(user, null))
        return false;

      return Equals(user);
    }

    public override int GetHashCode()
    {
      return _nick.GetHashCode();
    }

    public bool Equals(string nick)
    {
      return string.Equals(_nick, nick, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(User user)
    {
      if (ReferenceEquals(user, null))
        return false;

      if (ReferenceEquals(user, this))
        return true;

      return Equals(_nick, user.Nick);
    }
  }
}
