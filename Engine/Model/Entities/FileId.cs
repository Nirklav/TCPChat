using System;

namespace Engine.Model.Entities
{
  [Serializable]
  public struct FileId : IEquatable<FileId>
  {
    public readonly int Id;
    public readonly string Owner;

    public FileId(int id, string owner)
    {
      Id = id;
      Owner = owner;
    }

    public static bool operator == (FileId first, FileId second)
    {
      return first.Equals(second);
    }

    public static bool operator != (FileId first, FileId second)
    {
      return !first.Equals(second);
    }

    public override bool Equals(object obj)
    {
      if (obj == null)
        return false;
      if (!(obj is FileId))
        return false;
      return Equals((FileId)obj);
    }

    public bool Equals(FileId other)
    {
      return Id == other.Id && Owner == other.Owner;
    }

    public override int GetHashCode()
    {
      return (Id * 397) ^ Owner.GetHashCode();
    }
  }
}
