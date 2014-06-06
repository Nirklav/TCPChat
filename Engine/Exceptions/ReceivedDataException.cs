using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{
  /// <summary>
  /// Представляет ошибку получения данных.
  /// </summary>
  public class ReceivedDataException : Exception
  {
    const string message = "Получаемые данные имеют больший размер, чем максимально утновленное значение.";

    /// <summary>
    /// Инициализирует новый экземпляр класса.
    /// </summary>
    public ReceivedDataException()
      : base(message)
    {

    }
  }
}
