using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace Engine.Model.Common.Entities
{
  public sealed class PostedFile :
    MarshalByRefObject,
    IDisposable
  {
    private bool _disposed;

    /// <summary>
    /// File description.
    /// </summary>
    public FileDescription File
    {
      [SecuritySafeCritical]
      get;
      [SecuritySafeCritical]
      private set;
    }

    /// <summary>
    /// Rooms where file was posted.
    /// </summary>
    public HashSet<string> RoomNames
    {
      [SecuritySafeCritical]
      get;
      [SecuritySafeCritical]
      private set;
    }

    /// <summary>
    /// Opened file stream.
    /// </summary>
    public FileStream ReadStream
    {
      [SecuritySafeCritical]
      get;
      [SecuritySafeCritical]
      private set;
    }

    /// <summary>
    /// Creates the posted file instance.
    /// </summary>
    /// <param name="file">File description.</param>
    /// <param name="fileName">Full path to file.</param>
    [SecuritySafeCritical]
    public PostedFile(FileDescription file, string fileName)
    {
      File = file;
      RoomNames = new HashSet<string>();
      ReadStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
    }

    /// <summary>
    /// Dispose posted file resources.
    /// </summary>
    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;

      if (ReadStream != null)
        ReadStream.Dispose();
    }

    [SecuritySafeCritical]
    public bool Equals(PostedFile other)
    {
      if (ReferenceEquals(other, null))
        return false;
      if (ReferenceEquals(other, this))
        return true;
      return File.Equals(other.File);
    }

    [SecuritySafeCritical]
    public override bool Equals(object obj)
    {
      if (ReferenceEquals(obj, null))
        return false;
      if (ReferenceEquals(obj, this))
        return true;
      var other = obj as PostedFile;
      if (other == null)
        return false;
      return Equals(other);
    }

    [SecuritySafeCritical]
    public override int GetHashCode()
    {
      unchecked
      {
        return File.GetHashCode();
      }
    }
  }
}
