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
using System.Security.Cryptography;
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
      public WaitingCommandContainer(IPackage package, bool unreliable)
      {
        Package = package;
        Unreliable = unreliable;
      }

      public IPackage Package { get; private set; }
      public bool Unreliable { get; private set; }
    }
    #endregion

    #region consts
    public const string NetConfigString = "Peer TCPChat";
    public const int ConnectionTimeOut = 30 * 1000;
    private const int KeySize = 256;
    #endregion

    #region private fields
    private readonly object syncObject = new object();
    private Dictionary<string, List<WaitingCommandContainer>> waitingCommands;
    private Dictionary<string, byte[]> keys;
    private NetConnection serviceConnection;
    private NetPeer handler;
    private int state; //PeerState
    private bool disposed;
    private SynchronizationContext syncContext;
    private ClientRequestQueue requestQueue;
    private ECDiffieHellmanCng diffieHellman;
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
      keys = new Dictionary<string, byte[]>();
      syncContext = new EngineSyncContext();
      requestQueue = new ClientRequestQueue();

      diffieHellman = new ECDiffieHellmanCng(KeySize);
      diffieHellman.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
      diffieHellman.HashAlgorithm = CngAlgorithm.Sha256;
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
      syncContext.Send(RegisterReceived, handler);
      handler.Start();

      var hail = handler.CreateMessage();
      using (var client = ClientModel.Get())
      {
        var localPoint = new IPEndPoint(Connection.GetIpAddress(remotePoint.AddressFamily), handler.Port);

        hail.Write(client.User.Nick);
        hail.Write(localPoint);
      }

      serviceConnection = handler.Connect(remotePoint, hail);

      ClientModel.Logger.WriteDebug("AsyncPeer.ConnectToService({0})", remotePoint);
    }

    [SecurityCritical]
    private void RegisterReceived(object obj)
    {
      var server = (NetPeer)obj;
      server.RegisterReceivedCallback(OnReceive);
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

      handler.Connect(remotePoint, CreateHailMessage());
      
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
    /// <typeparam name="T">Тип параметра пакета.</typeparam>
    /// <param name="peerId">Идентификатор соединения, которому отправляется команда.</param>
    /// <param name="id">Индетификатор команды.</param>
    /// <param name="content">Параметр пакета.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    [SecuritySafeCritical]
    public void SendMessage<T>(string peerId, long id, T content, bool unreliable = false)
    {
      SendMessage(peerId, new Package<T>(id, content), unreliable);
    }

    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то вначале установит его.</b>
    /// </summary>
    /// <param name="peerId">Идентификатор соединения, которому отправляется команда.</param>
    /// <param name="package">Индетификатор пакета.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    [SecuritySafeCritical]
    public void SendMessage(string peerId, IPackage package, bool unreliable = false)
    {
      ThrowIfDisposed();

      if (handler == null || handler.Status != NetPeerStatus.Running)
      {
        SaveCommandAndConnect(peerId, package, unreliable);
        return;
      }

      var connection = FindConnection(peerId);
      if (connection == null)
      {
        SaveCommandAndConnect(peerId, package, unreliable);
        return;
      }

      var deliveryMethod = unreliable ? NetDeliveryMethod.Unreliable : NetDeliveryMethod.ReliableOrdered;
      var message = CreateMessage(peerId, package);
      handler.SendMessage(message, connection, deliveryMethod, 0);
    }
    #endregion

    #region Send message if connected
    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то команда отправлена не будет.</b>
    /// </summary>
    /// <typeparam name="T">Тип параметра пакета.</typeparam>
    /// <param name="peerId">Идентификатор соединения, которому отправляется команда.</param>
    /// <param name="id">Индетификатор команды.</param>
    /// <param name="content">Параметр команды.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    /// <returns>Отправлено ли сообщение.</returns>
    [SecuritySafeCritical]
    public bool SendMessageIfConnected<T>(string peerId, long id, T content, bool unreliable = false)
    {
      return SendMessageIfConnected(peerId, new Package<T>(id, content), unreliable);
    }

    /// <summary>
    /// Отправляет команду. <b>Если соединения нет, то команда отправлена не будет.</b>
    /// </summary>
    /// <param name="peerId">Идентификатор соединения, которому отправляется команда.</param>
    /// <param name="package">Индетификатор пакета.</param>
    /// <param name="unreliable">Отправить ненадежное сообщение. (быстрее)</param>
    /// <returns>Отправлено ли сообщение.</returns>
    [SecuritySafeCritical]
    public bool SendMessageIfConnected(string peerId, IPackage package, bool unreliable = false)
    {
      ThrowIfDisposed();

      if (handler == null || handler.Status != NetPeerStatus.Running)
        return false;

      var connection = FindConnection(peerId);
      if (connection == null)
        return false;

      var deliveryMethod = unreliable ? NetDeliveryMethod.Unreliable : NetDeliveryMethod.ReliableOrdered;
      var message = CreateMessage(peerId, package);
      handler.SendMessage(message, connection, deliveryMethod, 0);
      return true;
    }
    #endregion
    #endregion

    #region private methods
    [SecurityCritical]
    private byte[] GetKey(string peerId)
    {
      byte[] key;
      lock (syncObject)
        keys.TryGetValue(peerId, out key);

      if (key == null)
        throw new InvalidOperationException(string.Format("Key not set, for connection {0}", peerId));

      return key;
    }

    [SecurityCritical]
    private NetOutgoingMessage CreateMessage(string peerId, IPackage package)
    {
      byte[] pack;     
      using (var crypter = new Crypter())
      {
        crypter.SetKey(GetKey(peerId));
        pack = crypter.Encrypt(package);
      }

      var message = handler.CreateMessage(pack.Length);
      message.Write(pack);
      return message;
    }
    
    [SecurityCritical]
    private NetOutgoingMessage CreateHailMessage()
    {
      string nick;
      using (var client = ClientModel.Get())
        nick = client.User.Nick;

      var publicKeyBlob = diffieHellman.PublicKey.ToByteArray();
      var hailMessage = handler.CreateMessage();
      hailMessage.Write(nick);
      hailMessage.Write(publicKeyBlob.Length);
      hailMessage.Write(publicKeyBlob);

      return hailMessage;
    }

    [SecurityCritical]
    private void SaveCommandAndConnect(string peerId, IPackage package, bool unreliable)
    {
      lock (syncObject)
      {
        List<WaitingCommandContainer> commands;

        if (!waitingCommands.TryGetValue(peerId, out commands))
        {
          commands = new List<WaitingCommandContainer>();
          waitingCommands.Add(peerId, commands);
        }

        commands.Add(new WaitingCommandContainer(package, unreliable));
      }

      ClientModel.Api.ConnectToPeer(peerId);
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
    private void OnReceive(object obj)
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
            OnApprove(message);
            break;

          case NetIncomingMessageType.StatusChanged:
            NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

            if (status == NetConnectionStatus.Connected && state == (int)PeerState.ConnectedToPeers)
              OnPeerConnected(message);

            if (status == NetConnectionStatus.Connected && state == (int)PeerState.ConnectedToService)
              OnServiceConnected(message);

            if (status == NetConnectionStatus.Disconnecting || status == NetConnectionStatus.Disconnected)
              OnDisconnected(message);
            break;

          case NetIncomingMessageType.Data:
          case NetIncomingMessageType.UnconnectedData:
            if (state == (int)PeerState.ConnectedToPeers)
              OnPackageReceived(message);
            break;
        }
      }
    }

    [SecurityCritical]
    private void OnApprove(NetIncomingMessage message)
    {
      message.SenderConnection.Approve(CreateHailMessage());
      ClientModel.Logger.WriteDebug("AsyncPeer.Approve()");
    }

    [SecurityCritical]
    private void OnServiceConnected(NetIncomingMessage message)
    {
      ClientModel.Logger.WriteDebug("AsyncPeer.ServiceConnect()");
    }

    [SecurityCritical]
    private void OnPeerConnected(NetIncomingMessage message)
    {
      var hailMessage = message.SenderConnection.RemoteHailMessage;
      if (hailMessage == null)
      {
        hailMessage.SenderConnection.Deny();
        ClientModel.Logger.WriteWarning("ConnectionId is null [Message: {0}, SenderEndPoint: {1}]", message.ToString(), message.SenderEndPoint);
        return;
      }

      var connectionId = hailMessage.ReadString();
      var publicKeySize = hailMessage.ReadInt32();
      var publicKeyBlob = hailMessage.ReadBytes(publicKeySize);
      var publicKey = CngKey.Import(publicKeyBlob, CngKeyBlobFormat.EccPublicBlob);
      var key = diffieHellman.DeriveKeyMaterial(publicKey);

      message.SenderConnection.Tag = connectionId;

      lock (syncObject)
      {
        // Add connection key
        keys.Add(connectionId, key);

        // Invoke waiting commands
        List<WaitingCommandContainer> commands;
        if (waitingCommands.TryGetValue(connectionId, out commands))
        {
          foreach (WaitingCommandContainer command in commands)
            SendMessage(connectionId, command.Package, command.Unreliable);

          waitingCommands.Remove(connectionId);
        }
      }

      ClientModel.Logger.WriteDebug("AsyncPeer.PeerConnected({0})", connectionId);
    }

    [SecurityCritical]
    private void OnDisconnected(NetIncomingMessage message)
    {
      var connectionId = (string) message.SenderConnection.Tag;
      message.SenderConnection.Tag = null;

      if (connectionId != null)
        keys.Remove(connectionId);
    }

    [SecurityCritical]
    private void OnPackageReceived(NetIncomingMessage message)
    {
      try
      {
        var peerId = (string)message.SenderConnection.Tag;

        IPackage package;
        using (var crypter = new Crypter())
        {
          crypter.SetKey(GetKey(peerId));
          package = crypter.Decrypt<IPackage>(message.Data);
        }

        var command = ClientModel.Api.GetCommand(package.Id);
        var args = new ClientCommandArgs(peerId, package);

        requestQueue.Add(peerId, command, args);
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

      if (diffieHellman != null)
        diffieHellman.Dispose();

      lock (syncObject)
      {
        waitingCommands.Clear();
        keys.Clear();
      }
    }
    #endregion
  }
}
