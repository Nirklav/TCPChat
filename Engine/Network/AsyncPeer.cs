using Engine.Model.Client;
using Engine.Network;
using Engine.Network.Connections;
using Lidgren.Network;
using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Collections.Generic;
using Engine.Containers;
using Engine.Helpers;

namespace Engine.Network
{
  public enum PeerState
  {
    NotConnected = 0,
    ConnectedToService = 1,
    ConnectedToPeers = 2,
  }

  public class AsyncPeer : IDisposable
  {
    #region nested types

    private class WaitingCommandContainer
    {
      public WaitingCommandContainer(ushort id, object content)
        : this(id, content, false)
      { }

      public WaitingCommandContainer(ushort id, object content, bool unreliable)
      {
        CommandId = id;
        MessageContent = content;
        Unreliable = unreliable;
      }

      public ushort CommandId { get; set; }
      public object MessageContent { get; set; }
      public bool Unreliable { get; set; }
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
    private Dictionary<IPEndPoint, string> connectingTo;

    private NetConnection serviceConnection;

    private NetPeer handler;
    private DateTime lastActivity;
    private int state; //PeerState
    #endregion

    #region events and properties
    /// <summary>
    /// Состояние пира.
    /// </summary>
    public PeerState State
    {
      get { return (PeerState)state; }
    }
    #endregion

    #region constructor
    internal AsyncPeer()
    {
      waitingCommands = new Dictionary<string, List<WaitingCommandContainer>>();
      connectingTo = new Dictionary<IPEndPoint, string>();
    }
    #endregion

    #region public methods
    /// <summary>
    /// Подключение к P2P сервису. Для создания UDP окна.
    /// </summary>
    /// <param name="remotePoint">Адрес сервиса.</param>
    internal void ConnectToService(IPEndPoint remotePoint)
    {
      ThrowIfDisposed();

      if (Interlocked.CompareExchange(ref state, (int)PeerState.ConnectedToService, (int)PeerState.NotConnected) != (int)PeerState.NotConnected)
        throw new InvalidOperationException("Пир имеет неправильное состояние.");

      if (handler != null && handler.Status == NetPeerStatus.Running)
        throw new ArgumentException("уже запущен");

      NetPeerConfiguration config = new NetPeerConfiguration(NetConfigString);
      config.Port = 0;
      config.AcceptIncomingConnections = true;

      if (remotePoint.AddressFamily == AddressFamily.InterNetworkV6)
        config.LocalAddress = IPAddress.IPv6Any;

      handler = new NetPeer(config);

      if (SynchronizationContext.Current != null)
        handler.RegisterReceivedCallback(ReceivedCallback);
      else
      {
        SynchronizationContext.SetSynchronizationContext(new EngineSyncContext());
        handler.RegisterReceivedCallback(ReceivedCallback);
        SynchronizationContext.SetSynchronizationContext(null);
      }

      handler.Start();

      NetOutgoingMessage hailMessage = handler.CreateMessage();
      using (var client = ClientModel.Get())
      {
        IPEndPoint localPoint = new IPEndPoint(Connection.GetIPAddress(remotePoint.AddressFamily), handler.Port);

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
    internal void WaitConnection(IPEndPoint waitingPoint)
    {
      ThrowIfDisposed();

      int oldState = Interlocked.CompareExchange(ref state, (int)PeerState.ConnectedToPeers, (int)PeerState.ConnectedToService);
      if (oldState == (int)PeerState.NotConnected)
        throw new InvalidOperationException("Пир имеет неправильное состояние.");

      // Создания и отправка сообщения для пробивания NAT,
      // и возможности принять входящее соединение
      NetOutgoingMessage holePunchMessage = handler.CreateMessage();
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
    internal void ConnectToPeer(string peerId, IPEndPoint remotePoint)
    {
      ThrowIfDisposed();

      int oldState = Interlocked.CompareExchange(ref state, (int)PeerState.ConnectedToPeers, (int)PeerState.ConnectedToService);
      if (oldState == (int)PeerState.NotConnected)
        throw new InvalidOperationException("Пир имеет неправильное состояние.");

      if (handler == null)
        throw new InvalidOperationException("обработчик не создан");

      lock (connectingTo)
        connectingTo.Add(remotePoint, peerId);

      NetOutgoingMessage hailMessage = handler.CreateMessage();
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
    public bool IsConnected(string peerId)
    {
      ThrowIfDisposed();

      if (handler == null || handler.Status != NetPeerStatus.Running)
        return false;

      return handler.Connections.SingleOrDefault(c => string.Equals((string)c.Tag, peerId)) != null;
    }

    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то вначале установит его.</b>
    /// </summary>
    /// <param name="connectionId">Идентификатор соединения, которому отправляется команда.</param>
    /// <param name="commandId">Индетификатор команды.</param>
    /// <param name="messageContent">Параметр команды.</param>
    public void SendMessage(string connectionId, ushort commandId, object messageContent)
    {
      SendMessage(connectionId, commandId, messageContent, false);
    }

    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то вначале установит его.</b>
    /// </summary>
    /// <param name="peerId">Идентификатор соединения, которому отправляется команда.</param>
    /// <param name="commandId">Индетификатор команды.</param>
    /// <param name="messageContent">Параметр команды.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    public void SendMessage(string peerId, ushort commandId, object messageContent, bool unreliable)
    {
      ThrowIfDisposed();

      if (handler == null || handler.Status != NetPeerStatus.Running)
      {
        SaveCommandAndConnect(peerId, commandId, messageContent, unreliable);
        return;
      }

      NetConnection connection = handler.Connections.SingleOrDefault(c => string.Equals((string)c.Tag, peerId));
      if (connection == null)
      {
        SaveCommandAndConnect(peerId, commandId, messageContent, unreliable);
        return;
      }

      NetOutgoingMessage message = CreateMessage(commandId, messageContent);
      handler.SendMessage(message, connection, unreliable ? NetDeliveryMethod.Unreliable : NetDeliveryMethod.ReliableOrdered, 0);
    }

    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то команда отправлена не будет.</b>
    /// </summary>
    /// <param name="connectionId">Идентификатор соединения, которому отправляется команда.</param>
    /// <param name="commandId">Индетификатор команды.</param>
    /// <param name="messageContent">Параметр команды.</param>
    /// <returns>Отправлено ли сообщение.</returns>
    public bool SendMessageIfConnected(string connectionId, ushort commandId, object messageContent)
    {
      return SendMessageIfConnected(connectionId, commandId, messageContent, false);
    }

    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то команда отправлена не будет.</b>
    /// </summary>
    /// <param name="peerId">Идентификатор соединения, которому отправляется команда.</param>
    /// <param name="commandId">Индетификатор команды.</param>
    /// <param name="messageContent">Параметр команды.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    /// <returns>Отправлено ли сообщение.</returns>
    public bool SendMessageIfConnected(string peerId, ushort commandId, object messageContent, bool unreliable)
    {
      ThrowIfDisposed();

      if (handler == null || handler.Status != NetPeerStatus.Running)
        return false;

      NetConnection connection = handler.Connections.SingleOrDefault(c => string.Equals((string)c.Tag, peerId));
      if (connection == null)
        return false;

      NetOutgoingMessage message = CreateMessage(commandId, messageContent);
      handler.SendMessage(message, connection, unreliable ? NetDeliveryMethod.Unreliable : NetDeliveryMethod.ReliableOrdered, 0);

      return true;
    }

    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то команда отправлена не будет.</b>
    /// </summary>
    /// <param name="peerIds">Идентификаторы соединения, которым отправляется команда.</param>
    /// <param name="commandId">Индетификатор команды.</param>
    /// <param name="messageContent">Параметр команды.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    /// <returns>Отправлено ли сообщение.</returns>
    public void SendMessageIfConnected(IList<string> peerIds, ushort commandId, object messageContent, bool unreliable)
    {
      ThrowIfDisposed();

      if (handler == null || handler.Status != NetPeerStatus.Running)
        return;

      NetOutgoingMessage message = CreateMessage(commandId, messageContent);
      List<NetConnection> connections = handler.Connections.Where(c => peerIds.Contains((string)c.Tag)).ToList();

      if (connections.Count <= 0)
        return;

      handler.SendMessage(message, connections, unreliable ? NetDeliveryMethod.Unreliable : NetDeliveryMethod.ReliableOrdered, 0);
    }
    #endregion

    #region private methods
    private NetOutgoingMessage CreateMessage(ushort commandId, object messageContent)
    {
      NetOutgoingMessage message = handler.CreateMessage();

      if (messageContent != null)
        using (MemoryStream messageStream = new MemoryStream())
        {
          messageStream.Write(BitConverter.GetBytes(commandId), 0, sizeof(ushort));
          BinaryFormatter formatter = new BinaryFormatter();
          formatter.Serialize(messageStream, messageContent);
          message.Write(messageStream.ToArray());
        }

      return message;
    }

    private void SaveCommandAndConnect(string peerId, ushort commandId, object messageContent, bool unreliable)
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

    private void DisconnectFromService()
    {
      if (serviceConnection != null)
        serviceConnection.Disconnect(string.Empty);
    }
    #endregion

    #region callback method
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
            ClientModel.OnAsyncError(this, new AsyncErrorEventArgs { Error = new NetException(message.ReadString()) });
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

    private void PeerConnected(NetIncomingMessage message)
    {
      string connectionId;
      lock (connectingTo)
      {
        if (connectingTo.TryGetValue(message.SenderEndPoint, out connectionId))
          connectingTo.Remove(message.SenderEndPoint);
      }

      if (connectionId == null)
      {
        if (message.SenderConnection.RemoteHailMessage == null)
          return;

        connectionId = message.SenderConnection.RemoteHailMessage.ReadString();
      }

      if (connectionId == null)
      {
        ClientModel.Logger.WriteWarning("ConnectionId is null [Message: {0}]", message.ToString());
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

    private void DataReceived(NetIncomingMessage message)
    {
      lastActivity = DateTime.Now;

      try
      {
        IClientAPICommand command = ClientModel.API.GetCommand(message.Data);
        ClientCommandArgs args = new ClientCommandArgs
        {
          Message = message.Data,
          PeerConnectionId = (string)message.SenderConnection.Tag
        };

        command.Run(args);
      }
      catch (Exception exc)
      {
        ClientModel.OnAsyncError(this, new AsyncErrorEventArgs { Error = exc });
        ClientModel.Logger.Write(exc);
      }
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

      lock (waitingCommands)
        waitingCommands.Clear();

      lock (connectingTo)
        connectingTo.Clear();
    }
    #endregion
  }
}
