using System;
using System.Drawing;

namespace Engine.Model.Entities
{
  /// <summary>
  /// Описание пользователя.
  /// </summary>
  [Serializable]
  public class User
  {
    private string nick;
    private Color nickColor;

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
      if (obj == null)
        return false;

      if (!(obj is User))
        return false;

      return Equals((User)obj);
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
      if (user == null)
        return false;

      return Equals(Nick, user.Nick);
    }
  }
}
