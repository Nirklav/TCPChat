using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Containers
{
  /// <summary>
  /// Класс описывающий ожидающее отркытого ключа приватное сообщение.
  /// </summary>
  public class WaitingPrivateMessage
  {
    /// <summary>
    /// Получатель сообщения.
    /// </summary>
    public string Receiver { get; set; }

    /// <summary>
    /// Сообщение.
    /// </summary>
    public string Message { get; set; }
  }
}
