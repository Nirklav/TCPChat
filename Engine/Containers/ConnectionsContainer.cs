using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Engine.Connections;
using Engine.Network.Connections;

namespace Engine.Containers
{
  /// <summary>
  /// Класс хранящий соединения.
  /// </summary>
  public class ConnectionsContainer
  {
    /// <summary>
    /// Создает класс хранящий соединения.
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <param name="requestId">Соединение которое получит ответ.</param>
    /// <param name="senderId">Соединение которое прислало запрос.</param>
    public ConnectionsContainer(int id, string requestId, string senderId)
    {
      id = Id;
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

    /// <summary>
    /// Точка принимающая прямое соединение. У пользователя к которому хотят подключится.
    /// </summary>
    public IPEndPoint RequestPeerPoint { get; set; }

    /// <summary>
    /// Точка принимающая прямое соединение. У пользователя который запрашивает подключение.
    /// </summary>
    public IPEndPoint SenderPeerPoint { get; set; }

    /// <summary>
    /// Идентификатор пары соединений.
    /// </summary>
    public int Id { get; set; }
  }
}
