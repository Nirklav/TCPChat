using Engine.Api;
using Engine.Api.Server.P2P;
using Engine.Helpers;
using Engine.Model.Common.Entities;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security;
using System.Threading;

namespace Engine.Network
{
  public sealed class P2PService : IDisposable
  {
    #region nested types
    private class ClientDescription
    {
      public IPEndPoint LocalPoint { get; private set; }
      public IPEndPoint PublicPoint { get; private set; }

      public ClientDescription(IPEndPoint local, IPEndPoint @public)
      {
        LocalPoint = local;
        PublicPoint = @public;
      }
    }

    private class RequestPair
    {
      public UserId RequestId { get; private set; }
      public UserId SenderId { get; private set; }

      public RequestPair(UserId requestId, UserId senderId)
      {
        RequestId = requestId;
        SenderId = senderId;
      }
    }
    #endregion

    #region fields
    [SecurityCritical] private readonly object _syncObject = new object();

    [SecurityCritical] private readonly Dictionary<UserId, ClientDescription> _clientsEndPoints;
    [SecurityCritical] private readonly HashSet<UserId> _connectingClients;
    [SecurityCritical] private readonly List<RequestPair> _requests;
    
    [SecurityCritical] private readonly IApi _api;
    [SecurityCritical] private readonly Logger _logger;

    [SecurityCritical] private readonly NetServer _server;
    [SecurityCritical] private readonly SynchronizationContext _syncContext;

    [SecurityCritical] private bool _disposed;
    #endregion

    #region constructors
    /// <summary>
    /// Creates instance that responses for UDP hole punching.
    /// </summary>
    /// <param name="address">Address of p2p service.</param>
    /// <param name="port">UDP service port for input data.</param>
    /// <param name="api">Server api.</param>
    /// <param name="logger">Server logger.</param>
    [SecurityCritical]
    public P2PService(IPAddress address, int port, IApi api, Logger logger)
    {
      _clientsEndPoints = new Dictionary<UserId, ClientDescription>();
      _connectingClients = new HashSet<UserId>();
      _requests = new List<RequestPair>();

      _api = api;
      _logger = logger;
      
      var config = new NetPeerConfiguration(AsyncPeer.NetConfigString);
      config.MaximumConnections = 100;
      config.LocalAddress = address;
      config.Port = port;

      _server = new NetServer(config);
      _syncContext = new EngineSyncContext();
      _syncContext.Send(RegisterReceived, _server);
      _server.Start();
    }

    [SecurityCritical]
    private void RegisterReceived(object obj)
    {
      var server = (NetServer)obj;
      server.RegisterReceivedCallback(OnReceive);
    }
    #endregion

    #region properties
    /// <summary>
    /// Port service.
    /// </summary>
    public int Port
    {
      [SecuritySafeCritical]
      get
      {
        ThrowIfDisposed();
        return _server.Port;
      }
    }
    #endregion

    #region public methods
    /// <summary>
    /// Connects two users directly.
    /// </summary>
    /// <param name="senderId">Connection that sent request.</param>
    /// <param name="requestId">Connection that will receive response.</param>
    [SecuritySafeCritical]
    public void Introduce(UserId senderId, UserId requestId)
    {
      ThrowIfDisposed();

      if (requestId == UserId.Empty)
        throw new ArgumentNullException("requestId");

      if (senderId == UserId.Empty)
        throw new ArgumentNullException("senderId");

      if (TryDoneRequest(senderId, requestId))
        return;

      lock (_syncObject)
      {
        _requests.Add(new RequestPair(requestId, senderId));

        TrySendConnectRequest(senderId);
        TrySendConnectRequest(requestId);
      }
    }

    [SecurityCritical]
    private void TrySendConnectRequest(UserId connectionId)
    {
      bool needSend;
      lock (_syncObject)
      {
        needSend = !_clientsEndPoints.ContainsKey(connectionId);

        if (_connectingClients.Contains(connectionId))
          needSend = false;
        else if (needSend)
          _connectingClients.Add(connectionId);
      }

      if (needSend)
        _api.Perform(new ServerSendP2PConnectRequestAction(connectionId, Port));
    }

    [SecurityCritical]
    internal void RemoveEndPoint(UserId id)
    {
      lock (_syncObject)
      {
        _connectingClients.Remove(id);
        _clientsEndPoints.Remove(id);
      }
    }
    #endregion

    #region private methods
    [SecurityCritical]
    private void OnReceive(object obj)
    {
      NetIncomingMessage message;

      while ((message = _server.ReadMessage()) != null)
      {
        try
        {
          switch (message.MessageType)
          {
            case NetIncomingMessageType.ErrorMessage:
            case NetIncomingMessageType.WarningMessage:
              _logger.Write(new NetException(message.ReadString()));
              break;

            case NetIncomingMessageType.StatusChanged:
              var status = (NetConnectionStatus)message.ReadByte();

              if (status != NetConnectionStatus.Connected)
                break;

              var hailMessage = message.SenderConnection.RemoteHailMessage;
              if (hailMessage == null)
                continue;

              var id = AsyncPeer.ReadUserId(hailMessage);
              var localPoint = hailMessage.ReadIPEndPoint();
              var publicPoint = message.SenderEndPoint;

              lock (_syncObject)
              {
                _clientsEndPoints.Add(id, new ClientDescription(localPoint, publicPoint));

                TryDoneAllRequest();
              }
              break;
          }
        }
        catch (Exception e)
        {
          _logger.Write(e);
        }
      }
    }

    [SecurityCritical]
    private bool TryGetRequest(UserId senderId, UserId requestId, out IPEndPoint senderPoint, out IPEndPoint requestPoint)
    {
      var received = true;

      ClientDescription senderDescription;
      ClientDescription requestDescription;

      lock (_syncObject)
      {
        received &= _clientsEndPoints.TryGetValue(senderId, out senderDescription);
        received &= _clientsEndPoints.TryGetValue(requestId, out requestDescription);
      }

      if (received)
      {
        IPAddress senderAddress = senderDescription.PublicPoint.Address;
        IPAddress requestAddress = requestDescription.PublicPoint.Address;

        if (senderAddress.Equals(requestAddress))
        {
          senderPoint = senderDescription.LocalPoint;
          requestPoint = requestDescription.LocalPoint;
        }
        else
        {
          senderPoint = senderDescription.PublicPoint;
          requestPoint = requestDescription.PublicPoint;
        }
      }
      else
      {
        senderPoint = null;
        requestPoint = null;
      }

      return received;
    }

    [SecurityCritical]
    private bool TryDoneRequest(UserId senderId, UserId requestId)
    {
      IPEndPoint senderEndPoint;
      IPEndPoint requestEndPoint;
      if (TryGetRequest(senderId, requestId, out senderEndPoint, out requestEndPoint))
      {
        lock (_syncObject)
        {
          _requests.RemoveAll(p => p.SenderId == senderId && p.RequestId == requestId);

          _connectingClients.Remove(senderId);
          _connectingClients.Remove(requestId);
        }

        _api.Perform(new ServerIntroduceConnectionsAction(senderId, senderEndPoint, requestId, requestEndPoint));
        return true;
      }

      return false;
    }

    [SecurityCritical]
    private void TryDoneAllRequest()
    {
      lock (_syncObject)
      {
        List<UserId> removedIds = null;

        for (int i = _requests.Count - 1; i >= 0; i--)
        {
          IPEndPoint senderEndPoint;
          IPEndPoint requestEndPoint;

          var request = _requests[i];

          if (TryGetRequest(request.SenderId, request.RequestId, out senderEndPoint, out requestEndPoint))
          {
            (removedIds ?? (removedIds = new List<UserId>())).Add(request.SenderId);
            (removedIds ?? (removedIds = new List<UserId>())).Add(request.RequestId);

            _api.Perform(new ServerIntroduceConnectionsAction(request.SenderId, senderEndPoint, request.RequestId, requestEndPoint));
            _requests.RemoveAt(i);
          }
        }

        if (removedIds != null)
          foreach (var id in removedIds)
            _connectingClients.Remove(id);
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

      _server.Shutdown(string.Empty);

      lock (_syncObject)
      {
        _requests.Clear();
        _clientsEndPoints.Clear();
        _connectingClients.Clear();
      }
    }
    #endregion
  }
}
