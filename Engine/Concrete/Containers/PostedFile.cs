using Engine.Concrete.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Engine.Concrete.Containers
{
  /// <summary>
  /// Класс описывающий выложенный файл.
  /// </summary>
  public class PostedFile : IDisposable
  {
    /// <summary>
    /// Описание файла.
    /// </summary>
    public FileDescription File { get; set; }

    /// <summary>
    /// Комната в которую выложен файл.
    /// </summary>
    public string RoomName { get; set; }

    /// <summary>
    /// Поток для чтения файла.
    /// </summary>
    public FileStream ReadStream { get; set; }

    bool disposed = false;

    /// <summary>
    /// Освобождает ресурсы.
    /// </summary>
    public void Dispose()
    {
      if (disposed)
        return;

      disposed = true;

      if (ReadStream != null)
        ReadStream.Dispose();
    }
  }
}
