using Engine.Concrete.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Engine.Concrete.Containers
{
  /// <summary>
  /// Класс описывающий загружаемый файл.
  /// </summary>
  public class DownloadingFile : IDisposable
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
