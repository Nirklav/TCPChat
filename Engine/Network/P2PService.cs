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
      public ClientDescription(IPEndPoint local, IPEndPoint @public)
      {
        LocalPoint = local;
        PublicPoint = @public;
      }

      public IPEndPoint LocalPoint { get; private set; }
      public IPEndPoint PublicPoint { get; private set; }
    }

    private class RequestPair
    {
      public RequestPair(string requestId, string senderId)
      {
        RequestId = requestId;
        SenderId = senderId;
      }

      public string RequestId { get; private set; }
      public string SenderId { get; private set; }
    }
    #endregion

    #region fields
    [SecurityCritical] private readonly object syncObject = new object();

    [SecurityCritical] private readonly Dictionary<string, ClientDescription> clientsEndPoints;
    [SecurityCritical] private readonly HashSet<string> connectingClients;
    [SecurityCritical] private readonly List<RequestPair> requests;

    [SecurityCritical] private NetServer server;
    [SecurityCritical] private bool disposed;

    [SecurityCritical] private SynchronizationContext syncContext;
    #endregion

    #region constructors
    /// <summary>
    /// Создает экземпляр сервиса для функционирования UDP hole punching. Без логирования.
    /// </summary>
    [SecurityCritical]
    public P2PService(int port, bool usingIPv6)
    {
      disposed = false;
      requests = new List<RequestPair>();
      clientsEndPoints = new Dictionary<string, ClientDescription>();
      connectingClients = new HashSet<string>();

      NetPeerConfiguration config = new NetPeerConfiguration(AsyncPeer.NetConfigString);
      config.MaximumConnections = 100;
      config.Port = port;

      if (usingIPv6)
        config.LocalAddress = IPAddress.IPv6Any;

      syncContext = new EngineSyncContext();

      server = new NetServer(config);
      syncContext.Send(RegisterReceived, server);
      server.Start();
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
        return server.Port;
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

      lock (syncObject)
      {
        requests.Add(new RequestPair(requestId, senderId));

        TrySendConnectRequest(senderId);
        TrySendConnectRequest(requestId);
      }
    }

    [SecurityCritical]
    private void TrySendConnectRequest(string connectionId)
    {
      bool needSend = false;

      lock (syncObject)
      {
        needSend = !clientsEndPoints.ContainsKey(connectionId);

        if (connectingClients.Contains(connectionId))
          needSend = false;
        else if (needSend)
          connectingClients.Add(connectionId);
      }

      if (needSend)
        ServerModel.Api.SendP2PConnectRequest(connectionId, Port);
    }

    [SecurityCritical]
    internal void RemoveEndPoint(string id)
    {
      lock (syncObject)
        clientsEndPoints.Remove(id);
    }
    #endregion

    #region private methods
    [SecurityCritical]
    private void OnReceive(object obj)
    {
      NetIncomingMessage message;

      while ((message = server.ReadMessage()) != null)
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

              lock (syncObject)
              {
                clientsEndPoints.Add(id, new ClientDescription(localPoint, publicPoint));

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

      lock (syncObject)
      {
        received &= clientsEndPoints.TryGetValue(senderId, out senderDescription);
        received &= clientsEndPoints.TryGetValue(requestId, out requestDescription);
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
        lock (syncObject)
        {
          requests.RemoveAll(p => string.Equals(p.SenderId, senderId) && string.Equals(p.RequestId, requestId));

          connectingClients.Remove(senderId);
          connectingClients.Remove(requestId);
        }

        ServerModel.Api.IntroduceConnections(senderId, senderEndPoint, requestId, requestEndPoint);
      }

      return received;
    }

    [SecurityCritical]
    private void TryDoneAllRequest()
    {
      lock (syncObject)
      {
        List<string> removedIds = null;

        for (int i = requests.Count - 1; i >= 0; i--)
        {
          IPEndPoint senderEndPoint;
          IPEndPoint requestEndPoint;

          var request = requests[i];

          if (TryGetRequest(request.SenderId, request.RequestId, out senderEndPoint, out requestEndPoint))
          {
            (removedIds ?? (removedIds = new List<string>())).Add(request.SenderId);
            (removedIds ?? (removedIds = new List<string>())).Add(request.RequestId);

            ServerModel.Api.IntroduceConnections(request.SenderId, senderEndPoint, request.RequestId, requestEndPoint);
            requests.RemoveAt(i);
          }
        }

        if (removedIds != null)
          foreach (var id in removedIds)
            connectingClients.Remove(id);
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

      server.Shutdown(string.Empty);

      lock (syncObject)
      {
        requests.Clear();
        clientsEndPoints.Clear();
        connectingClients.Clear();
      }
    }
    #endregion
  }
}
