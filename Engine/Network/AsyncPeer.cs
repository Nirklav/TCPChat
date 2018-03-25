using Engine.Api;
using Engine.Api.Client.P2P;
using Engine.Helpers;
using Engine.Model.Client;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Engine.Network
{
  public enum PeerState
  {
    NotConnected = 0,
    ConnectedToService = 1,
    ConnectedToPeers = 2,
  }

  public sealed class AsyncPeer :
    MarshalByRefObject,
    IDisposable
  {
    #region nested types
    private enum HailMessageType
    {
      Connect = 0,
      Approve = 1
    }

    private sealed class RemotePeer
    {
      public readonly string Id;
      public readonly X509Certificate2 Certificate;
      public readonly Packer Packer;
      public readonly List<WaitingCommandContainer> WaitingCommands;

      [SecurityCritical]
      public RemotePeer(string id, X509Certificate2 certificate)
      {
        Id = id;
        Certificate = certificate;
        Packer = new Packer();
        WaitingCommands = new List<WaitingCommandContainer>();
      }
    }

    private sealed class WaitingCommandContainer
    {
      public readonly IPackage Package;
      public readonly byte[] RawData;
      public readonly bool Unreliable;

      public WaitingCommandContainer(IPackage package, byte[] rawData, bool unreliable)
      {
        Package = package;
        RawData = rawData;
        Unreliable = unreliable;
      }
    }
    #endregion

    #region consts
    public const string NetConfigString = "Peer TCPChat";
    #endregion

    #region private fields
    [SecurityCritical] private readonly object _syncObject = new object();

    [SecurityCritical] private readonly string _id;
    [SecurityCritical] private readonly X509Certificate2 _localCertificate;
    [SecurityCritical] private readonly Dictionary<string, RemotePeer> _peers;
    [SecurityCritical] private readonly SynchronizationContext _syncContext;
    [SecurityCritical] private readonly RequestQueue _requestQueue;
    [SecurityCritical] private readonly IApi _api;
    [SecurityCritical] private readonly IClientNotifier _notifier;
    [SecurityCritical] private readonly Logger _logger;

    [SecurityCritical] private NetConnection _serviceConnection;
    [SecurityCritical] private NetPeer _handler;

    [SecurityCritical] private int _state; //PeerState
    [SecurityCritical] private bool _disposed;
    #endregion

    #region events and properties
    /// <summary>
    /// Peer state.
    /// </summary>
    public PeerState State
    {
      [SecuritySafeCritical]
      get { return (PeerState)_state; }
    }
    #endregion

    #region constructor
    [SecurityCritical]
    internal AsyncPeer(string id, X509Certificate2 certificate, IApi api, IClientNotifier notifier, Logger logger)
    {
      _id = id;
      _localCertificate = certificate;
      _peers = new Dictionary<string, RemotePeer>();
      _syncContext = new EngineSyncContext();
      _requestQueue = new RequestQueue(api);
      _api = api;
      _notifier = notifier;
      _logger = logger;
    }
    #endregion

    #region public methods
    /// <summary>
    /// Registers peer.
    /// </summary>
    /// <param name="peerId">Peer id.</param>
    /// <param name="certificate">Peer certificate.</param>
    [SecurityCritical]
    internal void RegisterPeer(string peerId, X509Certificate2 certificate)
    {
      if (_id != peerId)
      {
        lock (_syncObject)
        {
          var peer = new RemotePeer(peerId, certificate);
          _peers.Add(peer.Id, peer);
        }
      }
    }

    /// <summary>
    /// Unregisters peer.
    /// </summary>
    /// <param name="peerId">Peer id.</param>
    [SecurityCritical]
    internal void UnregisterPeer(string peerId)
    {
      lock (_syncObject)
        _peers.Remove(peerId);
    }

    /// <summary>
    /// Connects to P2P service, for create UDP window.
    /// </summary>
    /// <param name="remotePoint">Service address.</param>
    [SecurityCritical]
    internal void ConnectToService(IPEndPoint remotePoint)
    {
      ThrowIfDisposed();

      if (Interlocked.CompareExchange(ref _state, (int)PeerState.ConnectedToService, (int)PeerState.NotConnected) != (int)PeerState.NotConnected)
        throw new InvalidOperationException("Peer has not right state.");

      if (_handler != null && _handler.Status == NetPeerStatus.Running)
        throw new ArgumentException("Already runned.");

      var config = new NetPeerConfiguration(NetConfigString);
      config.Port = 0;
      config.AcceptIncomingConnections = true;
      config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

      if (remotePoint.AddressFamily == AddressFamily.InterNetworkV6)
        config.LocalAddress = IPAddress.IPv6Any;

      _handler = new NetPeer(config);
      _syncContext.Send(RegisterReceived, _handler);
      _handler.Start();

      var hail = _handler.CreateMessage();
      var localPoint = new IPEndPoint(Connection.GetIpAddress(remotePoint.AddressFamily), _handler.Port);
      hail.Write(_id);
      hail.Write(localPoint);

      _serviceConnection = _handler.Connect(remotePoint, hail);

      _logger.WriteDebug("AsyncPeer.ConnectToService({0})", remotePoint);
    }

    [SecurityCritical]
    private void RegisterReceived(object obj)
    {
      var server = (NetPeer)obj;
      server.RegisterReceivedCallback(OnReceive);
    }

    /// <summary>
    /// Starts waiting for connection from other client.
    /// <remarks>Can be called only after service connection.</remarks>
    /// </summary>
    /// <param name="waitingPoint">End point where from other client will be connecting.</param>
    [SecurityCritical]
    internal void WaitConnection(IPEndPoint waitingPoint)
    {
      ThrowIfDisposed();

      int oldState = Interlocked.CompareExchange(ref _state, (int)PeerState.ConnectedToPeers, (int)PeerState.ConnectedToService);
      if (oldState == (int)PeerState.NotConnected)
        throw new InvalidOperationException("Peer has not right state.");

      // Creating and sending message that be pierce NAT 
      // and creates possibility accept incoming message
      var holePunchMessage = _handler.CreateMessage();
      holePunchMessage.Write((byte)0);
      _handler.SendUnconnectedMessage(holePunchMessage, waitingPoint);

      DisconnectFromService();

      _logger.WriteDebug("AsyncPeer.WaitConnection({0})", waitingPoint);
    }

    /// <summary>
    /// Connects to other peer.
    /// <remarks>Can be called only after service connection.</remarks>
    /// </summary>
    /// <param name="peerId">Peer id.</param>
    /// <param name="remotePoint">Peer address.</param>
    [SecurityCritical]
    internal void ConnectToPeer(string peerId, IPEndPoint remotePoint)
    {
      ThrowIfDisposed();

      int oldState = Interlocked.CompareExchange(ref _state, (int)PeerState.ConnectedToPeers, (int)PeerState.ConnectedToService);
      if (oldState == (int)PeerState.NotConnected)
        throw new InvalidOperationException("Peer has not right state.");

      if (_handler == null)
        throw new InvalidOperationException("Handler not created.");

      _handler.Connect(remotePoint, CreateConnectHailMessage(peerId));
      
      DisconnectFromService();

      _logger.WriteDebug("AsyncPeer.ConnectToPeer({0}, {1})", peerId, remotePoint);
    }

    /// <summary>
    /// Returns true if peer already connected.
    /// </summary>
    /// <param name="peerId">Peer id.</param>
    [SecuritySafeCritical]
    public bool IsConnected(string peerId)
    {
      ThrowIfDisposed();

      if (_handler == null || _handler.Status != NetPeerStatus.Running)
        return false;

      return FindConnection(peerId) != null;
    }

    #region Send message
    /// <summary>
    /// Sends command. <b>If connection not exist, then it will be created.</b>
    /// </summary>
    /// <param name="peerId">Peer id that will be receive message.</param>
    /// <param name="id">Command id.</param>
    /// <param name="content">Command content.</param>
    /// <param name="unreliable">Send unreliable message. (fastest)</param>
    [SecuritySafeCritical]
    public void SendMessage<T>(string peerId, long id, T content, bool unreliable = false)
    {
      SendMessage(peerId, new Package<T>(id, content), null, unreliable);
    }

    /// <summary>
    /// Sends command. <b>If connection not exist, then it will be created.</b>
    /// </summary>
    /// <param name="peerId">Peer id that will be receive message.</param>
    /// <param name="id">Command id.</param>
    /// <param name="content">Command content.</param>
    /// <param name="rawData">Data that not be serialized.</param>
    /// <param name="unreliable">Send unreliable message. (fastest)</param>
    [SecuritySafeCritical]
    public void SendMessage<T>(string peerId, long id, T content, byte[] rawData, bool unreliable = false)
    {
      SendMessage(peerId, new Package<T>(id, content), rawData, unreliable);
    }

    /// <summary>
    /// Sends command. <b>If connection not exist, then it will be created.</b>
    /// </summary>
    /// <param name="peerId">Peer id that will be receive message.</param>
    /// <param name="id">Command id.</param>
    /// <param name="unreliable">Send unreliable message. (fastest)</param>
    [SecuritySafeCritical]
    public void SendMessage(string peerId, long id, bool unreliable = false)
    {
      SendMessage(peerId, new Package(id), null, unreliable);
    }

    /// <summary>
    /// Sends command. <b>If connection not exist, then it will be created.</b>
    /// </summary>
    /// <param name="peerId">Peer id that will be receive message.</param>
    /// <param name="package">Package to send.</param>
    /// <param name="rawData">Data that not be serialized.</param>
    /// <param name="unreliable">Send unreliable message. (fastest)</param>
    [SecuritySafeCritical]
    public void SendMessage(string peerId, IPackage package, byte[] rawData, bool unreliable = false)
    {
      ThrowIfDisposed();

      if (_handler == null || _handler.Status != NetPeerStatus.Running)
      {
        SaveCommandAndConnect(peerId, package, rawData, unreliable);
        return;
      }

      var connection = FindConnection(peerId);
      if (connection == null)
      {
        SaveCommandAndConnect(peerId, package, rawData, unreliable);
        return;
      }

      var deliveryMethod = unreliable ? NetDeliveryMethod.Unreliable : NetDeliveryMethod.ReliableOrdered;
      var message = CreateMessage(peerId, package, rawData);
      _handler.SendMessage(message, connection, deliveryMethod, 0);
    }
    #endregion

    #region Send message if connected
    /// <summary>
    /// Sends command. <b>If connection not exist, then message will be skipped.</b>
    /// </summary>
    /// <param name="peerId">Peer id that will be receive message.</param>
    /// <param name="id">Command id.</param>
    /// <param name="content">Command content.</param>
    /// <param name="unreliable">Send unreliable message. (fastest)</param>
    /// <returns>Returns true if command was sent.</returns>
    [SecuritySafeCritical]
    public bool SendMessageIfConnected<T>(string peerId, long id, T content, bool unreliable = false)
    {
      return SendMessageIfConnected(peerId, new Package<T>(id, content), null, unreliable);
    }

    /// <summary>
    /// Sends command. <b>If connection not exist, then message will be skipped.</b>
    /// </summary>
    /// <param name="peerId">Peer id that will be receive message.</param>
    /// <param name="package">Индетификатор пакета.</param>
    /// <param name="rawData">Данные не требующие сериализации.</param>
    /// <param name="unreliable">Send unreliable message. (fastest)</param>
    /// <returns>Returns true if command was sent.</returns>
    [SecuritySafeCritical]
    public bool SendMessageIfConnected(string peerId, IPackage package, byte[] rawData, bool unreliable = false)
    {
      ThrowIfDisposed();

      if (_handler == null || _handler.Status != NetPeerStatus.Running)
        return false;

      var connection = FindConnection(peerId);
      if (connection == null)
        return false;

      var deliveryMethod = unreliable ? NetDeliveryMethod.Unreliable : NetDeliveryMethod.ReliableOrdered;
      var message = CreateMessage(peerId, package, rawData);
      _handler.SendMessage(message, connection, deliveryMethod, 0);
      return true;
    }
    #endregion
    #endregion

    #region private methods
    // Must be called under lock
    [SecurityCritical]
    private RemotePeer GetPeer(string peerId)
    {
      _peers.TryGetValue(peerId, out RemotePeer peer);
      if (peer == null)
        throw new InvalidOperationException(string.Format("Packer not set, for connection {0}", peerId));

      return peer;
    }

    [SecurityCritical]
    private NetOutgoingMessage CreateMessage(string peerId, IPackage package, byte[] rawData)
    {
      Packed packed;
      lock (_syncObject)
      {
        var peer = GetPeer(peerId);
        packed = peer.Packer.Pack(package, rawData);
      }
      
      var message = _handler.CreateMessage(packed.Length);
      message.Write(packed.Data, 0, packed.Length);
      return message;
    }

    [SecurityCritical]
    private NetOutgoingMessage CreateConnectHailMessage(string peerId)
    {
      byte[] key;
      lock (_syncObject)
      {
        var peer = GetPeer(peerId);
        using (var rng = new RNGCryptoServiceProvider())
        {
          var clearKey = new byte[32];
          rng.GetBytes(clearKey);

          var alg = peer.Certificate.PublicKey.Key;
          if (alg is RSACryptoServiceProvider rsa)
            key = rsa.Encrypt(clearKey, false);
          else
            throw new InvalidOperationException("not supported key algorithm");

          peer.Packer.SetKey(clearKey);
        }
      }

      var hailMessage = _handler.CreateMessage();
      hailMessage.Write(_id);
      hailMessage.Write((int)HailMessageType.Connect);
      hailMessage.Write(key.Length);
      hailMessage.Write(key);

      return hailMessage;
    }

    [SecurityCritical]
    private NetOutgoingMessage CreateApproveHailMessage()
    {
      var hailMessage = _handler.CreateMessage();
      hailMessage.Write(_id);
      hailMessage.Write((int)HailMessageType.Approve);
      return hailMessage;
    }

    [SecurityCritical]
    private void SaveCommandAndConnect(string peerId, IPackage package, byte[] rawData, bool unreliable)
    {
      lock (_syncObject)
      {
        var peer = GetPeer(peerId);
        peer.WaitingCommands.Add(new WaitingCommandContainer(package, rawData, unreliable));
      }

      _api.Perform(new ClientConnectToPeerAction(peerId));
    }

    [SecurityCritical]
    private void DisconnectFromService()
    {
      if (_serviceConnection != null)
      {
        _serviceConnection.Disconnect(string.Empty);
        _serviceConnection = null;
      }
    }

    [SecurityCritical]
    private NetConnection FindConnection(string id)
    {
      return _handler.Connections.SingleOrDefault(new Finder(id).Equals);
    }

    private class Finder
    {
      private readonly string _id;

      public Finder(string id)
      {
        _id = id;
      }

      [SecurityCritical]
      public bool Equals(NetConnection connection)
      {
        return string.Equals((string)connection.Tag, _id);
      }
    }
    #endregion

    #region callback method
    [SecurityCritical]
    private void OnReceive(object obj)
    {
      if (_handler == null || _handler.Status != NetPeerStatus.Running)
        return;

      NetIncomingMessage message;

      while ((message = _handler.ReadMessage()) != null)
      {
        switch (message.MessageType)
        {
          case NetIncomingMessageType.ErrorMessage:
          case NetIncomingMessageType.WarningMessage:
            var error = new NetException(message.ReadString());
            _notifier.AsyncError(new AsyncErrorEventArgs(error));
            break;

          case NetIncomingMessageType.ConnectionApproval:
            OnApprove(message);
            break;

          case NetIncomingMessageType.StatusChanged:
            NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

            if (status == NetConnectionStatus.Connected && _state == (int)PeerState.ConnectedToPeers)
              OnPeerConnected(message);

            if (status == NetConnectionStatus.Connected && _state == (int)PeerState.ConnectedToService)
              OnServiceConnected(message);

            if (status == NetConnectionStatus.Disconnecting || status == NetConnectionStatus.Disconnected)
              OnDisconnected(message);
            break;

          case NetIncomingMessageType.Data:
          case NetIncomingMessageType.UnconnectedData:
            if (_state == (int)PeerState.ConnectedToPeers)
              OnPackageReceived(message);
            break;
        }
      }
    }

    [SecurityCritical]
    private void OnApprove(NetIncomingMessage message)
    {
      message.SenderConnection.Approve(CreateApproveHailMessage());
      _logger.WriteDebug("AsyncPeer.Approve()");
    }

    [SecurityCritical]
    private void OnServiceConnected(NetIncomingMessage message)
    {
      _logger.WriteDebug("AsyncPeer.ServiceConnect()");
    }

    [SecurityCritical]
    private void OnPeerConnected(NetIncomingMessage message)
    {
      var hailMessage = message.SenderConnection.RemoteHailMessage;
      if (hailMessage == null)
      {
        message.SenderConnection.Deny();
        _logger.WriteWarning("Hail message is null [Message: {0}, SenderEndPoint: {1}]", message.ToString(), message.SenderEndPoint);
        return;
      }

      var peerId = hailMessage.ReadString();
      var hailMessageType = (HailMessageType)hailMessage.ReadInt32();

      message.SenderConnection.Tag = peerId;

      lock (_syncObject)
      {
        var peer = GetPeer(peerId);

        if (hailMessageType == HailMessageType.Connect)
        {
          var keySize = hailMessage.ReadInt32();
          var keyBlob = hailMessage.ReadBytes(keySize);

          byte[] clearKey;
          var alg = _localCertificate.PrivateKey;
          if (alg is RSACryptoServiceProvider rsa)
            clearKey = rsa.Decrypt(keyBlob, false);
          else
            throw new InvalidOperationException("not supported key algorithm");

          peer.Packer.SetKey(clearKey);
        }

        foreach (var command in peer.WaitingCommands)
          SendMessage(peerId, command.Package, command.RawData, command.Unreliable);
      }

      _logger.WriteDebug("AsyncPeer.PeerConnected({0})", peerId);
    }

    [SecurityCritical]
    private void OnDisconnected(NetIncomingMessage message)
    {
      var peerId = (string) message.SenderConnection.Tag;
      message.SenderConnection.Tag = null;

      if (peerId != null)
      {
        lock (_syncObject)
        {
          var peer = GetPeer(peerId);
          peer.Packer.ResetKey();
          peer.WaitingCommands.Clear();
        }
      }
    }

    [SecurityCritical]
    private void OnPackageReceived(NetIncomingMessage message)
    {
      try
      {
        lock (_syncObject)
        {
          var peerId = (string)message.SenderConnection.Tag;
          var peer = GetPeer(peerId);
          var unpacked = peer.Packer.Unpack<IPackage>(message.Data);

          _requestQueue.Add(peerId, unpacked);
        }
      }
      catch (Exception exc)
      {
        _notifier.AsyncError(new AsyncErrorEventArgs(exc));
        _logger.Write(exc);
      }
    }
    #endregion

    #region IDisposable
    [SecurityCritical]
    private void ThrowIfDisposed()
    {
      if (_disposed)
        throw new ObjectDisposedException("Object disposed");
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;

      if (_requestQueue != null)
        _requestQueue.Dispose();

      if (_handler != null)
        _handler.Shutdown(string.Empty);

      lock (_syncObject)
      {
        _peers.Clear();
      }
    }
    #endregion
  }
}
