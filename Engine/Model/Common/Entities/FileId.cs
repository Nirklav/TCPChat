using System;
using System.Security;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Model.Common.Entities
{
  [Serializable]
  [BinType("FieldIdDto")]
  public struct FileId : IEquatable<FileId>
  {
    [BinField("i")]
    private int _id;

    [BinField("o")]
    private UserId _owner;

    /// <summary>
    /// Create file identification.
    /// </summary>
    /// <param name="id">File identificator.</param>
    /// <param name="owner">File owner user id.</param>
    [SecuritySafeCritical]
    public FileId(int id, UserId owner)
    {
      _id = id;
      _owner = owner;
    }

    /// <summary>
    /// File identificator.
    /// </summary>
    public int Id { get { return _id; } }

    /// <summary>
    /// File owner user id.
    /// </summary>
    public UserId Owner {  get { return _owner; } }

    [SecuritySafeCritical]
    public static bool operator == (FileId first, FileId second)
    {
      return first.Equals(second);
    }

    [SecuritySafeCritical]
    public static bool operator != (FileId first, FileId second)
    {
      return !first.Equals(second);
    }

    [SecuritySafeCritical]
    public override bool Equals(object obj)
    {
      if (obj == null)
        return false;
      if (!(obj is FileId))
        return false;
      return Equals((FileId)obj);
    }

    [SecuritySafeCritical]
    public bool Equals(FileId other)
    {
      return Id == other.Id && Owner == other.Owner;
    }

    [SecuritySafeCritical]
    public override int GetHashCode()
    {
      return (Id * 397) ^ Owner.GetHashCode();
    }
  }
}
