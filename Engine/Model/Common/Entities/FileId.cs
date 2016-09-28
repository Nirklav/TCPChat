using System;
using System.Security;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Model.Common.Entities
{
  [Serializable]
  [BinType("FieldId")]
  public struct FileId : IEquatable<FileId>
  {
    [BinField("i")]
    public readonly int Id;

    [BinField("o")]
    public readonly string Owner;

    /// <summary>
    /// Create file identification.
    /// </summary>
    /// <param name="id">Identification.</param>
    /// <param name="owner">File owner nick.</param>
    [SecuritySafeCritical]
    public FileId(int id, string owner)
    {
      Id = id;
      Owner = owner;
    }

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
