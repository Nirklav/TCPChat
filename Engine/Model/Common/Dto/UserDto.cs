using Engine.Model.Common.Entities;
using System;
using System.Drawing;

namespace Engine.Model.Common.Dto
{
  /// <summary>
  /// User data transfer object.
  /// </summary>
  [Serializable]
  public class UserDto
  {
    public readonly string Nick;
    public readonly Color NickColor;

    public UserDto(string nick, Color color)
    {
      Nick = nick;
      NickColor = color;
    }
    
    public UserDto(User user)
    {
      Nick = user.Nick;
      NickColor = user.NickColor;
    }
  }
}
