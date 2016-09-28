using Engine.Model.Common.Entities;
using System;
using System.Drawing;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Model.Common.Dto
{
  /// <summary>
  /// User data transfer object.
  /// </summary>
  [Serializable]
  [BinType("UserDto")]
  public class UserDto
  {
    [BinField("n")]
    public string Nick;

    [BinField("c")]
    public ColorDto NickColor;

    public UserDto(User user)
      : this(user.Nick, user.NickColor)
    {
    }

    public UserDto(string nick, Color color)
    {
      Nick = nick;
      NickColor = new ColorDto(color);
    }
  }
}
