using System;
using System.Drawing;

namespace Engine.Model.Entities
{
  /// <summary>
  /// Описание пользователя.
  /// </summary>
  [Serializable]
  public class User : 
    IEquatable<User>,
    IEquatable<string>
  {
    private readonly string _nick;
    private readonly Color _nickColor;

    /// <summary>
    /// Создает описание пользователя.
    /// </summary>
    /// <param name="nick">Ник пользователя.</param>
    /// <param name="nickColor">Цвет ника.</param>
    public User(string nick, Color nickColor)
    {
      _nick = nick;
      _nickColor = nickColor;
    }

    /// <summary>
    /// Возвращает ник пользователя.
    /// </summary>
    public string Nick
    {
      get { return _nick; }
    }

    /// <summary>
    /// Цвет ника пользователя.
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
