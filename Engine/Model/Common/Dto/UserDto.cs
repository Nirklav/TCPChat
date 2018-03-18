using Engine.Model.Client.Entities;
using Engine.Model.Common.Entities;
using System;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Model.Common.Dto
{
  /// <summary>
  /// User data transfer object.
  /// </summary>
  [Serializable]
  [BinType("UserDto", Version = 2)]
  public class UserDto
  {
    [BinField("n")]
    public string Nick;

    [BinField("c")]
    public ColorDto NickColor;

    [BinField("i")]
    public byte[] Certificate;

    public UserDto(User user, byte[] certificate)
      : this(user.Nick, user.NickColor, certificate)
    {
    }

    public UserDto(ClientUser user)
      : this(user, user.Certificate.Export(X509ContentType.Cert))
    {

    }

    public UserDto(string nick, Color color, byte[] certificate)
    {
      Nick = nick;
      NickColor = new ColorDto(color);
      Certificate = certificate;
    }
  }
}
