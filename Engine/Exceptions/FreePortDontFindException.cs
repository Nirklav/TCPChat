using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{
  /// <summary>
  /// Представляет ошибку, которая возникает если свободный порт не найден.
  /// </summary>
  public class FreePortDontFindException : Exception
  {
    const string message = "Свободный порт не найден.";

    /// <summary>
    /// Инициализирует новый экземпляр класса.
    /// </summary>
    public FreePortDontFindException()
      : base(message)
    {

    }
  }
}
