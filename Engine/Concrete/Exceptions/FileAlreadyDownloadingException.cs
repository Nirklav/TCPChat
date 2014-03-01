using Engine.Concrete.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Concrete
{
  /// <summary>
  /// Представляет ошибку, которая возникает если файл уже загружается.
  /// </summary>
  public class FileAlreadyDownloadingException : Exception
  {
    const string message = "Файл уже загружается.";

    /// <summary>
    /// Описание файла который уже загружается.
    /// </summary>
    public FileDescription File { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса.
    /// </summary>
    /// <param name="file">Файл который уже существует.</param>
    public FileAlreadyDownloadingException(FileDescription file)
      : base(message)
    {
      File = file;
    }
  }
}
