using Engine.Helpers;
using Engine.Model.Server;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
    private readonly object syncObject = new object();

    private readonly Dictionary<string, ClientDescription> clientsEndPoints;
    private readonly HashSet<string> connectingClients;
    private readonly List<RequestPair> requests;

    private NetServer server;
    private bool disposed;

    private SynchronizationContext syncContext;
    #endregion

    #region constructors
    /// <summary>
    /// Создает экземпляр сервиса для функционирования UDP hole punching. Без логирования.
    /// </summary>
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
      syncContext.Send(s => ((NetServer)s).RegisterReceivedCallback(ReceivedCallback), server);
      server.Start();
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
    #endregion

    #region public methods
    /// <summary>
    /// Соединяет напрямую двух пользователей.
    /// </summary>
    /// <param name="senderId">Соединение которое прислало запрос.</param>
    /// <param name="requestId">Соединение получащее ответ.</param>
    public void Introduce(string senderId, string requestId)
    {
      ThrowIfDisposed();

      if (ServerModel.API == null)
        throw new ArgumentNullException("API");

      if (requestId == null)
        throw new ArgumentNullException("request");

      if (senderId == null)
        throw new ArgumentNullException("sender");

      if (TryDoneRequest(senderId, requestId))
        return;

      AddressFamily addressFamily = ServerModel.Server.UsingIPv6
        ? AddressFamily.InterNetworkV6
        : AddressFamily.InterNetwork;

      lock (syncObject)
      {
        requests.Add(new RequestPair(requestId, senderId));

        TrySendConnectRequest(senderId);
        TrySendConnectRequest(requestId);
      }
    }

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
        ServerModel.API.SendP2PConnectRequest(connectionId, Port);
    }

    internal void RemoveEndPoint(string id)
    {
      lock (syncObject)
        clientsEndPoints.Remove(id);
    }
    #endregion

    #region private methods
    private void ReceivedCallback(object obj)
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

        ServerModel.API.IntroduceConnections(senderId, senderEndPoint, requestId, requestEndPoint);
      }

      return received;
    }

    private void TryDoneAllRequest()
    {
      List<string> removedIds = null;

      lock (syncObject)
      {
        for (int i = requests.Count - 1; i >= 0; i--)
        {
          IPEndPoint senderEndPoint;
          IPEndPoint requestEndPoint;

          if (TryGetRequest(requests[i].SenderId, requests[i].RequestId, out senderEndPoint, out requestEndPoint))
          {
            (removedIds ?? (removedIds = new List<string>())).Add(requests[i].SenderId);
            (removedIds ?? (removedIds = new List<string>())).Add(requests[i].RequestId);

            ServerModel.API.IntroduceConnections(requests[i].SenderId, senderEndPoint, requests[i].RequestId, requestEndPoint);
            requests.RemoveAt(i);
          }
        }

        foreach (var id in removedIds)
          connectingClients.Remove(id);
      }
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
