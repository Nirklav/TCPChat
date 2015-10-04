using Engine.API;
using Engine.Helpers;
using Engine.Model.Client;
using Engine.Network.Connections;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;

namespace Engine.Network
{
  public enum PeerState
  {
    NotConnected = 0,
    ConnectedToService = 1,
    ConnectedToPeers = 2,
  }

  public class AsyncPeer :
    MarshalByRefObject,
    IDisposable
  {
    #region nested types

    private class WaitingCommandContainer
    {
      public WaitingCommandContainer(ushort commandId, byte[] messageContent)
        : this(commandId, messageContent, false)
      { }

      public WaitingCommandContainer(ushort commandId, byte[] messageContent, bool unreliable)
      {
        CommandId = commandId;
        MessageContent = messageContent;
        Unreliable = unreliable;
      }

      public ushort CommandId { get; private set; }
      public byte[] MessageContent { get; private set; }
      public bool Unreliable { get; private set; }
    }

    #endregion

    #region consts
    public const string NetConfigString = "Peer TCPChat";

    /// <summary>
    /// Время неактивности соединения, после прошествия которого соединение будет закрыто.
    /// </summary>
    public const int ConnectionTimeOut = 30 * 1000;
    #endregion

    #region private fields
    private Dictionary<string, List<WaitingCommandContainer>> waitingCommands;

    private NetConnection serviceConnection;

    private NetPeer handler;
    private int state; //PeerState
    private bool disposed;

    private SynchronizationContext syncContext;
    private ClientRequestQueue requestQueue;
    #endregion

    #region events and properties
    /// <summary>
    /// Состояние пира.
    /// </summary>
    public PeerState State
    {
      [SecuritySafeCritical]
      get { return (PeerState)state; }
    }
    #endregion

    #region constructor
    [SecurityCritical]
    internal AsyncPeer()
    {
      waitingCommands = new Dictionary<string, List<WaitingCommandContainer>>();

      syncContext = new EngineSyncContext();
      requestQueue = new ClientRequestQueue();
    }
    #endregion

    #region public methods
    /// <summary>
    /// Подключение к P2P сервису. Для создания UDP окна.
    /// </summary>
    /// <param name="remotePoint">Адрес сервиса.</param>
    [SecurityCritical]
    internal void ConnectToService(IPEndPoint remotePoint)
    {
      ThrowIfDisposed();

      if (Interlocked.CompareExchange(ref state, (int)PeerState.ConnectedToService, (int)PeerState.NotConnected) != (int)PeerState.NotConnected)
        throw new InvalidOperationException("Peer has not right state.");

      if (handler != null && handler.Status == NetPeerStatus.Running)
        throw new ArgumentException("Already runned.");

      var config = new NetPeerConfiguration(NetConfigString);
      config.Port = 0;
      config.AcceptIncomingConnections = true;
      config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

      if (remotePoint.AddressFamily == AddressFamily.InterNetworkV6)
        config.LocalAddress = IPAddress.IPv6Any;

      handler = new NetPeer(config);
      syncContext.Send(RegisterReceivedCallback, handler);
      handler.Start();

      var hailMessage = handler.CreateMessage();
      using (var client = ClientModel.Get())
      {
        var localPoint = new IPEndPoint(Connection.GetIpAddress(remotePoint.AddressFamily), handler.Port);

        hailMessage.Write(client.User.Nick);
        hailMessage.Write(localPoint);
      }

      serviceConnection = handler.Connect(remotePoint, hailMessage);

      ClientModel.Logger.WriteDebug("AsyncPeer.ConnectToService({0})", remotePoint);
    }

    /// <summary>
    /// Ожидать подключение другого клиента.
    /// <remarks>Возможно вызвать только после подключения к сервису.</remarks>
    /// </summary>
    /// <param name="waitingPoint">Конечная точка, от которой следует ждать подключение.</param>
    [SecurityCritical]
    internal void WaitConnection(IPEndPoint waitingPoint)
    {
      ThrowIfDisposed();

      int oldState = Interlocked.CompareExchange(ref state, (int)PeerState.ConnectedToPeers, (int)PeerState.ConnectedToService);
      if (oldState == (int)PeerState.NotConnected)
        throw new InvalidOperationException("Peer has not right state.");

      // Создания и отправка сообщения для пробивания NAT,
      // и возможности принять входящее соединение
      var holePunchMessage = handler.CreateMessage();
      holePunchMessage.Write((byte)0);
      handler.SendUnconnectedMessage(holePunchMessage, waitingPoint);

      DisconnectFromService();

      ClientModel.Logger.WriteDebug("AsyncPeer.WaitConnection({0})", waitingPoint);
    }

    /// <summary>
    /// Подключение к другому клиенту.
    /// <remarks>Возможно вызвать только после подключения к сервису.</remarks>
    /// </summary>
    /// <param name="peerId">Id пира к которому подключаемся.</param>
    /// <param name="remotePoint">Адрес клиента</param>
    [SecurityCritical]
    internal void ConnectToPeer(string peerId, IPEndPoint remotePoint)
    {
      ThrowIfDisposed();

      int oldState = Interlocked.CompareExchange(ref state, (int)PeerState.ConnectedToPeers, (int)PeerState.ConnectedToService);
      if (oldState == (int)PeerState.NotConnected)
        throw new InvalidOperationException("Peer has not right state.");

      if (handler == null)
        throw new InvalidOperationException("Handler not created.");

      var hailMessage = handler.CreateMessage();
      using (var client = ClientModel.Get())
        hailMessage.Write(client.User.Nick);

      handler.Connect(remotePoint, hailMessage);
      
      DisconnectFromService();

      ClientModel.Logger.WriteDebug("AsyncPeer.ConnectToPeer({0}, {1})", peerId, remotePoint);
    }

    /// <summary>
    /// Имеется ли соедиенение к данному пиру.
    /// </summary>
    /// <param name="peerId">Id соединения.</param>
    [SecuritySafeCritical]
    public bool IsConnected(string peerId)
    {
      ThrowIfDisposed();

      if (handler == null || handler.Status != NetPeerStatus.Running)
        return false;

      return FindConnection(peerId) != null;
    }

    #region Send message
    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то вначале установит его.</b>
    /// </summary>
    /// <param name="peerId">Идентификатор соединения, которому отправляется команда.</param>
    /// <param name="commandId">Индетификатор команды.</param>
    /// <param name="messageContent">Параметр команды.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    [SecuritySafeCritical]
    public void SendMessage(string peerId, ushort commandId, object messageContent, bool unreliable = false)
    {
      SendMessage(peerId, commandId, Serializer.Serialize(messageContent), unreliable);
    }

    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то вначале установит его.</b>
    /// </summary>
    /// <param name="peerId">Идентификатор соединения, которому отправляется команда.</param>
    /// <param name="commandId">Индетификатор команды.</param>
    /// <param name="messageContent">Параметр команды.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    [SecuritySafeCritical]
    public void SendMessage(string peerId, ushort commandId, byte[] messageContent, bool unreliable = false)
    {
      ThrowIfDisposed();

      if (handler == null || handler.Status != NetPeerStatus.Running)
      {
        SaveCommandAndConnect(peerId, commandId, messageContent, unreliable);
        return;
      }

      var connection = FindConnection(peerId);
      if (connection == null)
      {
        SaveCommandAndConnect(peerId, commandId, messageContent, unreliable);
        return;
      }

      var message = CreateMessage(commandId, messageContent);
      handler.SendMessage(message, connection, unreliable ? NetDeliveryMethod.Unreliable : NetDeliveryMethod.ReliableOrdered, 0);
    }
    #endregion

    #region Send message if connected
    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то команда отправлена не будет.</b>
    /// </summary>
    /// <param name="peerId">Идентификатор соединения, которому отправляется команда.</param>
    /// <param name="commandId">Индетификатор команды.</param>
    /// <param name="messageContent">Параметр команды.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    /// <returns>Отправлено ли сообщение.</returns>
    [SecuritySafeCritical]
    public bool SendMessageIfConnected(string peerId, ushort commandId, object messageContent, bool unreliable = false)
    {
      return SendMessageIfConnected(peerId, commandId, Serializer.Serialize(messageContent), unreliable);
    }

    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то команда отправлена не будет.</b>
    /// </summary>
    /// <param name="peerId">Идентификатор соединения, которому отправляется команда.</param>
    /// <param name="commandId">Индетификатор команды.</param>
    /// <param name="messageContent">Сериализованный параметр команды.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    /// <returns>Отправлено ли сообщение.</returns>
    [SecuritySafeCritical]
    public bool SendMessageIfConnected(string peerId, ushort commandId, byte[] messageContent, bool unreliable = false)
    {
      ThrowIfDisposed();

      if (handler == null || handler.Status != NetPeerStatus.Running)
        return false;

      var connection = FindConnection(peerId);
      if (connection == null)
        return false;

      var message = CreateMessage(commandId, messageContent);
      handler.SendMessage(message, connection, unreliable ? NetDeliveryMethod.Unreliable : NetDeliveryMethod.ReliableOrdered, 0);
      return true;
    }
    #endregion

    #region SendMessage if connected (multiple peers)
    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то команда отправлена не будет.</b>
    /// </summary>
    /// <param name="peerIds">Идентификаторы соединения, которым отправляется команда.</param>
    /// <param name="commandId">Индетификатор команды.</param>
    /// <param name="messageContent">Параметр команды.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    /// <returns>Отправлено ли сообщение.</returns>
    [SecuritySafeCritical]
    public void SendMessageIfConnected(IList<string> peerIds, ushort commandId, object messageContent, bool unreliable = false)
    {
      SendMessageIfConnected(peerIds, commandId, Serializer.Serialize(messageContent), unreliable);
    }

    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то команда отправлена не будет.</b>
    /// </summary>
    /// <param name="peerIds">Идентификаторы соединения, которым отправляется команда.</param>
    /// <param name="commandId">Индетификатор команды.</param>
    /// <param name="messageContent">Сериализованный параметр команды.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    /// <returns>Отправлено ли сообщение.</returns>
    [SecuritySafeCritical]
    public void SendMessageIfConnected(IList<string> peerIds, ushort commandId, byte[] messageContent, bool unreliable = false)
    {
      ThrowIfDisposed();

      if (handler == null || handler.Status != NetPeerStatus.Running)
        return;

      var connections = FindConnections(peerIds);
      if (connections.Count <= 0)
        return;

      var message = CreateMessage(commandId, messageContent);
      handler.SendMessage(message, connections, unreliable ? NetDeliveryMethod.Unreliable : NetDeliveryMethod.ReliableOrdered, 0);
    }
    #endregion
    #endregion

    #region private methods
    [SecurityCritical]
    private NetOutgoingMessage CreateMessage(ushort commandId, byte[] messageContent)
    {
      var message = handler.CreateMessage();

      message.Write(commandId);

      if (messageContent != null)
        message.Write(messageContent);

      return message;
    }

    [SecurityCritical]
    private void SaveCommandAndConnect(string peerId, ushort commandId, byte[] messageContent, bool unreliable)
    {
      lock (waitingCommands)
      {
        List<WaitingCommandContainer> commands;

        if (!waitingCommands.TryGetValue(peerId, out commands))
        {
          commands = new List<WaitingCommandContainer>();
          waitingCommands.Add(peerId, commands);
        }

        commands.Add(new WaitingCommandContainer(commandId, messageContent, unreliable));
      }

      ClientModel.API.ConnectToPeer(peerId);
    }

    [SecurityCritical]
    private void DisconnectFromService()
    {
      if (serviceConnection != null)
      {
        serviceConnection.Disconnect(string.Empty);
        serviceConnection = null;
      }
    }

    [SecurityCritical]
    private void RegisterReceivedCallback(object obj)
    {
      var server = (NetPeer)obj;
      server.RegisterReceivedCallback(ReceivedCallback);
    }

    [SecurityCritical]
    private NetConnection FindConnection(string id)
    {
      return handler.Connections.SingleOrDefault(new Finder(id).Equals);
    }

    [SecurityCritical]
    private List<NetConnection> FindConnections(IEnumerable<string> ids)
    {
      return handler.Connections.Where(new Finder(ids).Contains).ToList();
    }

    private class Finder
    {
      private string id;
      private IEnumerable<string> ids;

      public Finder(string id)
      {
        this.id = id;
      }

      public Finder(IEnumerable<string> ids)
      {
        this.ids = ids;
      }

      [SecurityCritical]
      public bool Equals(NetConnection connection)
      {
        return string.Equals((string)connection.Tag, id);
      }

      [SecurityCritical]
      public bool Contains(NetConnection connection)
      {
        return ids.Contains((string)connection.Tag);
      }
    }
    #endregion

    #region callback method
    [SecurityCritical]
    private void ReceivedCallback(object obj)
    {
      if (handler == null || handler.Status != NetPeerStatus.Running)
        return;

      NetIncomingMessage message;

      while ((message = handler.ReadMessage()) != null)
      {
        switch (message.MessageType)
        {
          case NetIncomingMessageType.ErrorMessage:
          case NetIncomingMessageType.WarningMessage:
            ClientModel.Notifier.AsyncError(new AsyncErrorEventArgs { Error = new NetException(message.ReadString()) });
            break;

          case NetIncomingMessageType.ConnectionApproval:
            Approve(message);
            break;

          case NetIncomingMessageType.StatusChanged:
            NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

            if (status == NetConnectionStatus.Connected && state == (int)PeerState.ConnectedToPeers)
              PeerConnected(message);

            if (status == NetConnectionStatus.Connected && state == (int)PeerState.ConnectedToService)
              ClientModel.Logger.WriteDebug("AsyncPeer.ServiceConnect()");

            if (status == NetConnectionStatus.Disconnecting || status == NetConnectionStatus.Disconnected)
              message.SenderConnection.Tag = null;
            break;

          case NetIncomingMessageType.Data:
          case NetIncomingMessageType.UnconnectedData:
            if (state == (int)PeerState.ConnectedToPeers)
              DataReceived(message);
            break;
        }
      }
    }

    [SecurityCritical]
    private void Approve(NetIncomingMessage message)
    {
      var userNick = string.Empty;
      using (var client = ClientModel.Get())
        userNick = client.User.Nick;

      var hailMessage = handler.CreateMessage(userNick);
      message.SenderConnection.Approve(hailMessage);

      ClientModel.Logger.WriteDebug("AsyncPeer.Approve({0})", userNick);
    }

    [SecurityCritical]
    private void PeerConnected(NetIncomingMessage message)
    {
      string connectionId = null;

      if (message.SenderConnection.RemoteHailMessage != null)
        connectionId = message.SenderConnection.RemoteHailMessage.ReadString();

      if (connectionId == null)
      {
        ClientModel.Logger.WriteWarning("ConnectionId is null [Message: {0}, SenderEndPoint: {1}]", message.ToString(), message.SenderEndPoint);
        return;
      }

      message.SenderConnection.Tag = connectionId;

      lock (waitingCommands)
      {
        List<WaitingCommandContainer> commands;
        if (waitingCommands.TryGetValue(connectionId, out commands))
        {
          foreach (WaitingCommandContainer command in commands)
            SendMessage(connectionId, command.CommandId, command.MessageContent, command.Unreliable);

          waitingCommands.Remove(connectionId);
        }
      }

      ClientModel.Logger.WriteDebug("AsyncPeer.PeerConnected({0})", connectionId);
    }

    [SecurityCritical]
    private void DataReceived(NetIncomingMessage message)
    {
      try
      {
        var peerConnectionId = (string)message.SenderConnection.Tag;
        var command = ClientModel.API.GetCommand(message.Data);
        var args = new ClientCommandArgs
        {
          Message = message.Data,
          PeerConnectionId = peerConnectionId
        };

        requestQueue.Add(peerConnectionId, command, args);
      }
      catch (Exception exc)
      {
        ClientModel.Notifier.AsyncError(new AsyncErrorEventArgs { Error = exc });
        ClientModel.Logger.Write(exc);
      }
    }
    #endregion

    #region IDisposable
    [SecurityCritical]
    private void ThrowIfDisposed()
    {
      if (disposed)
        throw new ObjectDisposedException("Object disposed");
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (disposed)
        return;

      disposed = true;

      if (requestQueue != null)
        requestQueue.Dispose();

      if (handler != null)
        handler.Shutdown(string.Empty);

      lock (waitingCommands)
        waitingCommands.Clear();
    }
    #endregion
  }
}
