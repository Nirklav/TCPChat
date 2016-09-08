using Engine.Helpers;
using Engine.Model.Server;
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
      public string RequestId { get; private set; }
      public string SenderId { get; private set; }

      public RequestPair(string requestId, string senderId)
      {
        RequestId = requestId;
        SenderId = senderId;
      }
    }
    #endregion

    #region fields
    [SecurityCritical] private readonly object _syncObject = new object();

    [SecurityCritical] private readonly Dictionary<string, ClientDescription> _clientsEndPoints;
    [SecurityCritical] private readonly HashSet<string> _connectingClients;
    [SecurityCritical] private readonly List<RequestPair> _requests;

    [SecurityCritical] private NetServer _server;
    [SecurityCritical] private bool _disposed;

    [SecurityCritical] private SynchronizationContext _syncContext;
    #endregion

    #region constructors
    /// <summary>
    /// Создает экземпляр сервиса для функционирования UDP hole punching.
    /// </summary>
    /// <param name="port">UDP порт сервиса. Для входящих данных.</param>
    /// <param name="usingIPv6">Использовать IPv6.</param>
    [SecurityCritical]
    public P2PService(int port, bool usingIPv6)
    {
      _disposed = false;
      _requests = new List<RequestPair>();
      _clientsEndPoints = new Dictionary<string, ClientDescription>();
      _connectingClients = new HashSet<string>();

      NetPeerConfiguration config = new NetPeerConfiguration(AsyncPeer.NetConfigString);
      config.MaximumConnections = 100;
      config.Port = port;

      if (usingIPv6)
        config.LocalAddress = IPAddress.IPv6Any;

      _syncContext = new EngineSyncContext();

      _server = new NetServer(config);
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
    /// Порт сервиса.
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
    /// Соединяет напрямую двух пользователей.
    /// </summary>
    /// <param name="senderId">Соединение которое прислало запрос.</param>
    /// <param name="requestId">Соединение получащее ответ.</param>
    [SecuritySafeCritical]
    public void Introduce(string senderId, string requestId)
    {
      ThrowIfDisposed();

      if (ServerModel.Api == null)
        throw new ArgumentNullException("API");

      if (requestId == null)
        throw new ArgumentNullException("request");

      if (senderId == null)
        throw new ArgumentNullException("sender");

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
    private void TrySendConnectRequest(string connectionId)
    {
      bool needSend = false;

      lock (_syncObject)
      {
        needSend = !_clientsEndPoints.ContainsKey(connectionId);

        if (_connectingClients.Contains(connectionId))
          needSend = false;
        else if (needSend)
          _connectingClients.Add(connectionId);
      }

      if (needSend)
        ServerModel.Api.SendP2PConnectRequest(connectionId, Port);
    }

    [SecurityCritical]
    internal void RemoveEndPoint(string id)
    {
      lock (_syncObject)
        _clientsEndPoints.Remove(id);
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
              ServerModel.Logger.Write(new NetException(message.ReadString()));
              break;

            case NetIncomingMessageType.StatusChanged:
              var status = (NetConnectionStatus)message.ReadByte();

              if (status != NetConnectionStatus.Connected)
                break;

              var hailMessage = message.SenderConnection.RemoteHailMessage;
              if (hailMessage == null)
                continue;

              var id = hailMessage.ReadString();
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
          ServerModel.Logger.Write(e);
        }
      }
    }

    [SecurityCritical]
    private bool TryGetRequest(string senderId, string requestId, out IPEndPoint senderPoint, out IPEndPoint requestPoint)
    {
      bool received = true;

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
    private bool TryDoneRequest(string senderId, string requestId)
    {
      bool received;
      IPEndPoint senderEndPoint;
      IPEndPoint requestEndPoint;

      if (received = TryGetRequest(senderId, requestId, out senderEndPoint, out requestEndPoint))
      {
        lock (_syncObject)
        {
          _requests.RemoveAll(p => string.Equals(p.SenderId, senderId) && string.Equals(p.RequestId, requestId));

          _connectingClients.Remove(senderId);
          _connectingClients.Remove(requestId);
        }

        ServerModel.Api.IntroduceConnections(senderId, senderEndPoint, requestId, requestEndPoint);
      }

      return received;
    }

    [SecurityCritical]
    private void TryDoneAllRequest()
    {
      lock (_syncObject)
      {
        List<string> removedIds = null;

        for (int i = _requests.Count - 1; i >= 0; i--)
        {
          IPEndPoint senderEndPoint;
          IPEndPoint requestEndPoint;

          var request = _requests[i];

          if (TryGetRequest(request.SenderId, request.RequestId, out senderEndPoint, out requestEndPoint))
          {
            (removedIds ?? (removedIds = new List<string>())).Add(request.SenderId);
            (removedIds ?? (removedIds = new List<string>())).Add(request.RequestId);

            ServerModel.Api.IntroduceConnections(request.SenderId, senderEndPoint, request.RequestId, requestEndPoint);
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
