using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

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
    public User(string Nick)
    {
      nick = Nick;
    }

    /// <summary>
    /// Возвращает ник пользователя.
    /// </summary>
    public string Nick
    {
      get { return nick; }
      set { nick = value; }
    }

    /// <summary>
    /// Цвет ника пользователя.
    /// </summary>
    public Color NickColor
    {
      get { return nickColor; }
      set { nickColor = value; }
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
      return string.Equals(Nick, userNick);
    }

    public bool Equals(User user)
    {
      if (user == null)
        return false;

      return string.Equals(Nick, user.Nick);
    }
  }
}
