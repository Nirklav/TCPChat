using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Engine.Concrete.Connections;

namespace Engine.Concrete.Containers
{
    /// <summary>
    /// Класс хранящий соединения.
    /// </summary>
    public class ConnectionsContainer
    {
        /// <summary>
        /// Создает класс хранящий соединения.
        /// </summary>
        /// <param name="requestConnection">Соединение которое получит ответ.</param>
        /// <param name="sendedConnection">Соединение которое прислало запрос.</param>
        public ConnectionsContainer(ServerConnection requestConnection, ServerConnection senderConnection)
        {
            RequestConnection = requestConnection;
            SenderConnection = senderConnection;
        }

        /// <summary>
        /// Соединение у которого запрашивают прямое соединение.
        /// </summary>
        public ServerConnection RequestConnection { get; set; }

        /// <summary>
        /// Соединение запрашивающее прямое соединение.
        /// </summary>
        public ServerConnection SenderConnection { get; set; }

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
