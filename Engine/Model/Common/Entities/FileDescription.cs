using System;

namespace Engine.Model.Common.Entities
{
  [Serializable]
  public class FileDescription : IEquatable<FileDescription>
  {
    private readonly FileId _id;
    private readonly long _size;
    private readonly string _name;

    /// <summary>
    /// Create new file description instance.
    /// </summary>
    /// <param name="id">File identification.</param>
    /// <param name="size">File size.</param>
    /// <param name="name">File name.</param>
    public FileDescription(FileId id, long size, string name)
    {
      _id = id;
      _size = size;
      _name = name;
    }

    /// <summary>
    /// File identification.
    /// </summary>
    public FileId Id
    {
      get { return _id; }
    }

    /// <summary>
    /// File name.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// File size.
    /// </summary>
    public long Size
    {
      get { return _size; }
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(obj, null))
        return false;

      if (ReferenceEquals(obj, this))
        return true;

      var file = obj as FileDescription;
      if (ReferenceEquals(file, null))
        return false;

      return Equals(file);
    }

    public bool Equals(FileDescription file)
    {
      if (ReferenceEquals(file, null))
        return false;

      if (ReferenceEquals(file, this))
        return true;

      return _id == file._id;
    }

    public override int GetHashCode()
    {
      return _id.GetHashCode();
    }
  }
}
