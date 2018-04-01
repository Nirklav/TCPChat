using Engine.Model.Common.Dto;
using System;
using System.Drawing;
using System.Security;

namespace Engine.Model.Common.Entities
{
  // TODO: add dispose
  [Serializable]
  public class User : 
    IEquatable<User>
  {
    private readonly UserId _id;
    private readonly Color _nickColor;

    /// <summary>
    /// Creates new instance of user.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <param name="nickColor">Nick color.</param>
    [SecuritySafeCritical]
    public User(UserId id, Color nickColor)
    {
      _id = id;
      _nickColor = nickColor;
    }

    /// <summary>
    /// Creates new instance of user.
    /// </summary>
    /// <param name="dto">Data transfer object of user.</param>
    [SecuritySafeCritical]
    public User(UserDto dto)
    {
      _id = dto.Id;
      _nickColor = dto.NickColor.ToColor();
    }

    /// <summary>
    /// User id.
    /// </summary>
    public UserId Id
    {
      [SecuritySafeCritical]
      get { return _id; }
    }

    /// <summary>
    /// Nick color.
    /// </summary>
    public Color NickColor
    {
      [SecuritySafeCritical]
      get { return _nickColor; }
    }

    [SecuritySafeCritical]
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

    [SecuritySafeCritical]
    public override int GetHashCode()
    {
      return _id.GetHashCode();
    }

    [SecuritySafeCritical]
    public bool Equals(User user)
    {
      if (ReferenceEquals(user, null))
        return false;

      if (ReferenceEquals(user, this))
        return true;

      return Equals(_id, user.Id);
    }
  }
}
