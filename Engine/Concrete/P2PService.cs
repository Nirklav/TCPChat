using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Engine.Concrete.Connections;
using Engine.Concrete.Helpers;
using Engine.Concrete.Entities;
using Engine.Concrete.Containers;
using Engine.Abstract;

namespace Engine.Concrete
{
  public enum ConnectionType : byte
  {
    Sender = 0,
    Request = 1
  }

  public sealed class P2PService : IDisposable
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
    public P2PService()
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
    /// <param name="logger">Логгер.</param>
    public P2PService(Logger logger)
      : this()
    {
      this.logger = logger;
    }

    /// <summary>
    /// Создает экземпляр сервиса для функционирования UDP hole punching.
    /// </summary>
    /// <param name="api">API использующиеся для "знакомства" пиров.</param>
    /// <param name="logger">Логгер.</param>
    public P2PService(IServerAPI api, Logger logger)
      : this(logger)
    {
      API = api;
    }
    #endregion

    #region properties
    /// <summary>
    /// Порт сервиса.
    /// </summary>
    public int Port
    {
      get
      {
        ThrowIfDisposed();
        return server.Port;
      }
    }

    /// <summary>
    /// API использующиеся для "знакомства" пиров.
    /// </summary>
    public IServerAPI API { get; set; }
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
      ThrowIfDisposed();

      if (API == null)
        throw new ArgumentNullException("API");

      if (request == null)
        throw new ArgumentNullException("request");

      if (sender == null)
        throw new ArgumentNullException("sender");

      int id = 0;

      lock (waitingsConnections)
      {
        while (waitingsConnections.Keys.Contains(id))
          id = idCreator.Next(int.MinValue, int.MaxValue);

        waitingsConnections.Add(id, new ConnectionsContainer(request, sender) { Id = id });
      }

      return id;
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

              API.IntroduceConnections(container);
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

    #region IDisposable
    private void ThrowIfDisposed()
    {
      if (disposed)
        throw new ObjectDisposedException("Object disposed");
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
  }
}
