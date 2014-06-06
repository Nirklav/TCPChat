using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{
  /// <summary>
  /// Представляет ошибку, которая возникает если API не поддерживается.
  /// </summary>
  public class APINotSupprtedException : Exception
  {
    const string message = "Приложение не поддерживает эту версию API. Соединение разорвано.";

    /// <summary>
    /// Инициализирует новый экземпляр класса.
    /// </summary>
    /// <param name="serverAPI">Версия API которая не поддерживается.</param>
    public APINotSupprtedException(string serverAPI)
      : base(message)
    {
      ServerAPIVersion = serverAPI;
    }

    /// <summary>
    /// Версия которую использует сервер.
    /// </summary>
    public string ServerAPIVersion { get; private set; }
  }
}
