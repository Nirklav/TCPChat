using Engine.Concrete.Entities;
using Lidgren.Network;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Engine.Concrete.Connections
{
  public class PeerConnection : IDisposable
  {
    #region consts
    public const string NetConfigString = "PeerConnection TCPChat";

    /// <summary>
    /// Время неактивности соединения, после прошествия которого соединение будет закрыто.
    /// </summary>
    public const int ConnectionTimeOut = 30 * 1000;
    #endregion

    #region private fields
    NetPeer handler;
    DateTime lastActivity;
    User info;
    int port;
    int connectId;
    SynchronizationContext context;
    #endregion

    #region events and properties
    /// <summary>
    /// Описание удаленного пользователя.
    /// </summary>
    public User Info
    {
      get
      {
        ThrowIfDisposed();
        return info;
      }
      set
      {
        ThrowIfDisposed();
        info = value;
      }
    }

    /// <summary>
    /// Интервал нективности подключения.
    /// </summary>
    public int IntervalOfSilence
    {
      get
      {
        ThrowIfDisposed();
        return (int)(DateTime.Now - lastActivity).TotalMilliseconds;
      }
    }

    /// <summary>
    /// Подключено ли соединение к пиру.
    /// </summary>
    public bool ConnectedToPeer
    {
      get
      {
        ThrowIfDisposed();
        return info != null && handler != null && handler.ConnectionsCount > 0;
      }
    }

    public int ConnectId
    {
      get
      {
        ThrowIfDisposed();
        return connectId;
      }
    }

    /// <summary>
    /// Происходит при получении пакета данных.
    /// </summary>
    public event EventHandler<DataReceivedEventArgs> DataReceived;

    /// <summary>
    /// Происходит при установлении соединения, или получении ошибки при соединении.
    /// </summary>
    public event EventHandler<ConnectEventArgs> Connected;

    /// <summary>
    /// Происходит при разъединении.
    /// </summary>
    public event EventHandler<ConnectEventArgs> Disconnected;

    /// <summary>
    /// Происходит при асинхронной ошибке, не воаращаемой в других событиях.
    /// </summary>
    public event EventHandler<AsyncErrorEventArgs> AsyncError;
    #endregion

    #region constructor
    public PeerConnection()
    {
      context = new SynchronizationContext();
    }
    #endregion

    #region public methods
    /// <summary>
    /// Подключение к P2P сервису. Для создания UDP окна.
    /// </summary>
    /// <param name="remotePoint">Адрес сервиса.</param>
    /// <param name="serviceConnectId">Id для соединения с сервисом.</param>
    public void ConnectToService(IPEndPoint remotePoint, int serviceConnectId, ConnectionType type)
    {
      ThrowIfDisposed();

      if (SynchronizationContext.Current == null)
        SynchronizationContext.SetSynchronizationContext(context);

      if (handler != null && handler.Status == NetPeerStatus.Running)
        throw new ArgumentException("уже запущен");

      NetPeerConfiguration config = new NetPeerConfiguration(NetConfigString);
      config.MaximumConnections = 1;
      config.Port = 0;
      if (remotePoint.AddressFamily == AddressFamily.InterNetworkV6)
        config.LocalAddress = IPAddress.IPv6Any;

      handler = new NetPeer(config);
      handler.RegisterReceivedCallback(ServiceDataReceivedCallback);
      handler.Start();

      connectId = serviceConnectId;

      NetOutgoingMessage hailMessage = handler.CreateMessage(4);
      hailMessage.Write(serviceConnectId);
      hailMessage.Write((byte)type);
      handler.Connect(remotePoint, hailMessage);
    }

    /// <summary>
    /// Ожидать подключение другого клиента. Возможно вызвать только после подключения к сервису.
    /// </summary>
    /// <param name="waitingPoint">Конечная точка, от которой следует ждать подключение.</param>
    public void WaitConnection(IPEndPoint waitingPoint)
    {
      ThrowIfDisposed();

      if (SynchronizationContext.Current == null)
        SynchronizationContext.SetSynchronizationContext(context);

      if (handler != null && handler.Status == NetPeerStatus.Running)
        throw new ArgumentException("уже запущен");

      NetPeerConfiguration config = new NetPeerConfiguration(NetConfigString);
      config.MaximumConnections = 1;
      config.AcceptIncomingConnections = true;
      config.Port = port;
      if (waitingPoint.AddressFamily == AddressFamily.InterNetworkV6)
        config.LocalAddress = IPAddress.IPv6Any;

      handler = new NetPeer(config);
      handler.RegisterReceivedCallback(PeerDataReceivedCallback);
      handler.Start();

      NetOutgoingMessage holePunchMessage = handler.CreateMessage();
      holePunchMessage.Write((byte)0);
      handler.SendUnconnectedMessage(holePunchMessage, waitingPoint);
    }

    /// <summary>
    /// Подключение к другому клиенту.
    /// </summary>
    /// <param name="remotePoint">Адрес клиента</param>
    public void ConnectToPeer(IPEndPoint remotePoint)
    {
      ThrowIfDisposed();

      if (SynchronizationContext.Current == null)
        SynchronizationContext.SetSynchronizationContext(context);

      if (handler != null && handler.Status == NetPeerStatus.Running)
        throw new ArgumentException("уже запущен");

      NetPeerConfiguration config = new NetPeerConfiguration(NetConfigString);
      config.Port = port;
      config.MaximumConnections = 1;
      if (remotePoint.AddressFamily == AddressFamily.InterNetworkV6)
        config.LocalAddress = IPAddress.IPv6Any;

      handler = new NetPeer(config);
      handler.RegisterReceivedCallback(PeerDataReceivedCallback);
      handler.Start();
      handler.Connect(remotePoint);
    }

    /// <summary>
    /// Отправляет команду.
    /// </summary>
    /// <param name="id">Индетификатор команды.</param>
    /// <param name="messageContent">Параметр команды.</param>
    public void SendMessage(ushort id, object messageContent)
    {
      ThrowIfDisposed();

      if (handler == null ||
          handler.ConnectionsCount <= 0 ||
          handler.Status != NetPeerStatus.Running)
        throw new ArgumentException("не соединен");

      MemoryStream messageStream = new MemoryStream();
      messageStream.Write(BitConverter.GetBytes(id), 0, sizeof(ushort));

      if (messageContent != null)
      {
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(messageStream, messageContent);
      }

      NetOutgoingMessage message = handler.CreateMessage();
      message.Write(messageStream.ToArray());
      messageStream.Dispose();
      handler.SendMessage(message, handler.Connections, NetDeliveryMethod.ReliableOrdered, 0);
    }

    /// <summary>
    /// Закрывает существующее соединение.
    /// </summary>
    public void Disconnect()
    {
      ThrowIfDisposed();

      if (handler != null && handler.Status == NetPeerStatus.Running)
        handler.Shutdown(string.Empty);

      while (handler.Status != NetPeerStatus.NotRunning)
        Thread.Sleep(100);

      info = null;
    }
    #endregion

    #region callback methods
    private void PeerDataReceivedCallback(object obj)
    {
      NetIncomingMessage message;
      NetPeer peer = (NetPeer)obj;

      while ((message = peer.ReadMessage()) != null)
      {
        switch (message.MessageType)
        {
          case NetIncomingMessageType.ErrorMessage:
          case NetIncomingMessageType.WarningMessage:
            OnAsyncError(new AsyncErrorEventArgs() { Error = new NetException(message.ReadString()) });
            break;

          case NetIncomingMessageType.StatusChanged:
            NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();
            if (status == NetConnectionStatus.Connected)
              OnConnect(new ConnectEventArgs());

            if (status == NetConnectionStatus.Disconnected)
              OnDisconnect(new ConnectEventArgs());

            break;

          case NetIncomingMessageType.Data:
            lastActivity = DateTime.Now;
            OnDataReceived(new DataReceivedEventArgs() { ReceivedData = message.Data });
            break;
        }
      }
    }

    private void ServiceDataReceivedCallback(object obj)
    {
      NetIncomingMessage message;
      NetPeer peer = (NetPeer)obj;

      while ((message = peer.ReadMessage()) != null)
      {
        switch (message.MessageType)
        {
          case NetIncomingMessageType.ErrorMessage:
          case NetIncomingMessageType.WarningMessage:
            OnAsyncError(new AsyncErrorEventArgs() { Error = new NetException(message.ReadString()) });
            break;

          case NetIncomingMessageType.StatusChanged:
            NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

            if (status == NetConnectionStatus.Connected)
              port = peer.Port;
            break;
        }
      }
    }
    #endregion

    #region private methods
    private void OnAsyncError(AsyncErrorEventArgs args)
    {
      EventHandler<AsyncErrorEventArgs> temp = Interlocked.CompareExchange<EventHandler<AsyncErrorEventArgs>>(ref AsyncError, null, null);

      if (temp != null)
        temp(this, args);
    }

    private void OnConnect(ConnectEventArgs args)
    {
      EventHandler<ConnectEventArgs> temp = Interlocked.CompareExchange<EventHandler<ConnectEventArgs>>(ref Connected, null, null);

      if (temp != null)
        temp(this, args);
    }

    private void OnDisconnect(ConnectEventArgs args)
    {
      EventHandler<ConnectEventArgs> temp = Interlocked.CompareExchange<EventHandler<ConnectEventArgs>>(ref Disconnected, null, null);

      if (temp != null)
        temp(this, args);
    }

    private void OnDataReceived(DataReceivedEventArgs args)
    {
      EventHandler<DataReceivedEventArgs> temp = Interlocked.CompareExchange<EventHandler<DataReceivedEventArgs>>(ref DataReceived, null, null);

      if (temp != null)
        temp(this, args);
    }
    #endregion

    #region IDisposable
    private bool disposed = false;

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

      if (handler != null && handler.Status == NetPeerStatus.Running)
        handler.Shutdown(string.Empty);
    }
    #endregion
  }
}
