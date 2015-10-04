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
    private readonly string nick;
    private readonly Color nickColor;

    /// <summary>
    /// Создает описание пользователя.
    /// </summary>
    /// <param name="Nick">Ник пользователя.</param>
    public User(string Nick, Color color)
    {
      nick = Nick;
      nickColor = color;
    }

    /// <summary>
    /// Возвращает ник пользователя.
    /// </summary>
    public string Nick
    {
      get { return nick; }
    }

    /// <summary>
    /// Цвет ника пользователя.
    /// </summary>
    public Color NickColor
    {
      get { return nickColor; }
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
      return nick.GetHashCode();
    }

    public bool Equals(string userNick)
    {
      return string.Equals(Nick, userNick, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(User user)
    {
      if (ReferenceEquals(user, null))
        return false;

      if (ReferenceEquals(user, this))
        return true;

      return Equals(Nick, user.Nick);
    }
  }
}
