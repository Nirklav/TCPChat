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

    bool disposed = false;

    /// <summary>
    /// Освобождает ресурсы.
    /// </summary>
    public void Dispose()
    {
      if (disposed)
        return;

      disposed = true;

      if (WriteStream != null)
        WriteStream.Dispose();
    }
  }
}
