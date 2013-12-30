using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TCPChat.Engine.API.StandartAPI;
using TCPChat.Engine.Connections;

namespace TCPChat.Engine
{
    public enum ConnectionType : byte
    {
        Sender = 0,
        Request = 1
    }

    class P2PService : IDisposable
    {
        #region private values
        Dictionary<int, ConnectionsContainer> waitingsConnections;
        SynchronizationContext context;
        NetServer server;
        Logger logger;
        Random idCreator;
        bool disposed;
        #endregion

        #region constructors
        /// <summary>
        /// Создает экземпляр сервиса для функционирования UDP hole punching. Без логирования.
        /// </summary>
        /// <param name="IPv6">Нужно ли использовать IPv6 (если нет, будет использован IPv4)</param>
        public P2PService(bool IPv6)
        {
            disposed = false;

            idCreator = new Random();
            waitingsConnections = new Dictionary<int, ConnectionsContainer>();

            NetPeerConfiguration config = new NetPeerConfiguration(PeerConnection.NetConfigString);
            config.MaximumConnections = 100;

            context = new SynchronizationContext();

            if (SynchronizationContext.Current == null)
                SynchronizationContext.SetSynchronizationContext(context);

            server = new NetServer(config);
            server.RegisterReceivedCallback(DataReceivedCallback);
            server.Start();
        }

        /// <summary>
        /// Создает экземпляр сервиса для функционирования UDP hole punching.
        /// </summary>
        /// <param name="IPv6">Нужно ли использовать IPv6 (если нет то будет использован IPv4).</param>
        /// <param name="logger">Логгер.</param>
        public P2PService(bool IPv6, Logger logger) : this(IPv6)
        {
            this.logger = logger;
        }
        #endregion

        #region properties
        public int Port
        {
            get { return server.Port; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Начинает ожидать соединение.
        /// </summary>
        /// <param name="request">Соединение получащее ответ.</param>
        /// <param name="sender">Соединение которое прислало запрос.</param>
        /// <returns>Вовзращает индетификатор который следуюет отправить подключениям, для опознания.
        /// Пользователю запрашивающему подключение вторым параметром следуюет отправить ConnectionType.Sender.
        /// Пользователю у которого запрашивают соединение вторым параметром следует ConnectionType.Request.
        /// </returns>
        public int WaitConnection(ServerConnection request, ServerConnection sender)
        {
            int id = 0;

            lock (waitingsConnections)
            {
                while (waitingsConnections.Keys.Contains(id))
                    id = idCreator.Next(int.MinValue, int.MaxValue);

                waitingsConnections.Add(id, new ConnectionsContainer(request, sender));
            }

            return id;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            server.Shutdown(string.Empty);

            lock (waitingsConnections)
                waitingsConnections.Clear();
        }
        #endregion

        #region private methods
        private void DataReceivedCallback(object obj)
        {
            NetIncomingMessage message;

            while ((message = server.ReadMessage()) != null)
            {
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.ErrorMessage:
                    case NetIncomingMessageType.WarningMessage:
                        if (logger != null)
                            logger.Write(new NetException(message.ReadString()));
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

                        if (status != NetConnectionStatus.Connected)
                            break;

                        int id = message.SenderConnection.RemoteHailMessage.ReadInt32();
                        ConnectionType type = (ConnectionType)message.SenderConnection.RemoteHailMessage.ReadByte();

                        ConnectionsContainer container;
                        lock (waitingsConnections)
                            container = waitingsConnections[id];

                        if (type == ConnectionType.Sender)
                            container.SenderPeerPoint = message.SenderEndPoint;
                        else
                            container.RequestPeerPoint = message.SenderEndPoint;

                        if (container.RequestPeerPoint != null && container.SenderPeerPoint != null)
                        {
                            lock (waitingsConnections)
                                waitingsConnections.Remove(id);

                            ClientWaitPeerConnectionCommand.MessageContent content = new ClientWaitPeerConnectionCommand.MessageContent();
                            content.RequestPoint = container.RequestPeerPoint;
                            content.SenderPoint = container.SenderPeerPoint;
                            content.RemoteInfo = container.SenderConnection.Info;
                            content.ServiceConnectId = id;

                            container.RequestConnection.SendMessage(ClientWaitPeerConnectionCommand.Id, content);
                        }
                        break;
                }
            }
        }

        private void AsyncErrorCallback(object sender, AsyncErrorEventArgs e)
        {
            if (logger != null)
                logger.Write(e.Error);
        }
        #endregion
    }
}
