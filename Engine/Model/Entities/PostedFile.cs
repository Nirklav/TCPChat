using System;
using System.IO;

namespace Engine.Model.Entities
{
  /// <summary>
  /// Класс описывающий выложенный файл.
  /// </summary>
  public sealed class PostedFile :
    MarshalByRefObject,
    IDisposable
  {
    private bool _disposed;

    /// <summary>
    /// Комната в которую выложен файл.
    /// </summary>
    public string RoomName { get; set; }

    /// <summary>
    /// Описание файла.
    /// </summary>
    public FileDescription File { get; set; }

    /// <summary>
    /// Поток для чтения файла.
    /// </summary>
    public FileStream ReadStream { get; set; }

    /// <summary>
    /// Освобождает ресурсы.
    /// </summary>
    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;

      if (ReadStream != null)
        ReadStream.Dispose();
    }
  }
}
