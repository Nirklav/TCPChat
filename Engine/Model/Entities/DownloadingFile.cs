using System;
using System.IO;

namespace Engine.Model.Entities
{
  /// <summary>
  /// Класс описывающий загружаемый файл.
  /// </summary>
  public sealed class DownloadingFile : 
    MarshalByRefObject, 
    IDisposable
  {
    private bool _disposed = false;

    /// <summary>
    /// Описание файла.
    /// </summary>
    public FileDescription File { get; set; }

    /// <summary>
    /// Поток для сохранения файла.
    /// </summary>
    public FileStream WriteStream { get; set; }

    /// <summary>
    /// Полное имя файла.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// Освобождает ресурсы.
    /// </summary>
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
