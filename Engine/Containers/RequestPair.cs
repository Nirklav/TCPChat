using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Engine.Network.Connections;

namespace Engine.Containers
{
  /// <summary>
  /// Класс хранящий соединения.
  /// </summary>
  public class RequestPair
  {
    /// <summary>
    /// Создает класс хранящий соединения.
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <param name="requestId">Соединение которое получит ответ.</param>
    /// <param name="senderId">Соединение которое прислало запрос.</param>
    public RequestPair(string requestId, string senderId)
    {
      RequestId = requestId;
      SenderId = senderId;
    }

    /// <summary>
    /// Соединение у которого запрашивают прямое соединение.
    /// </summary>
    public string RequestId { get; set; }

    /// <summary>
    /// Соединение запрашивающее прямое соединение.
    /// </summary>
    public string SenderId { get; set; }
  }
}
