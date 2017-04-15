using System;
using System.IO;
using System.Security;

namespace Engine.Model.Common.Entities
{
  public sealed class DownloadingFile : 
    MarshalByRefObject, 
    IDisposable
  {
    private bool _disposed = false;

    /// <summary>
    /// File Description.
    /// </summary>
    public FileDescription File
    {
      [SecuritySafeCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Saving file stream.
    /// </summary>
    public FileStream WriteStream
    {
      [SecuritySafeCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Path where file stored.
    /// </summary>
    public string FullName
    {
      [SecuritySafeCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Create the instance of DownloadingFile.
    /// </summary>
    /// <param name="file">File description.</param>
    /// <param name="fullName">Path where the file will be stored on disc.</param>
    [SecuritySafeCritical]
    public DownloadingFile(FileDescription file, string fullName)
    {
      File = file;
      FullName = fullName;
      WriteStream = new FileStream(fullName, FileMode.Create, FileAccess.Write);
    }

    /// <summary>
    /// Dispose the resources of DownloadingFile.
    /// </summary>
    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;

      if (WriteStream != null)
      {
        WriteStream.Dispose();
        WriteStream = null;
      }
    }
  }
}
