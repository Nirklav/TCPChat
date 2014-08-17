using Engine.API.StandardAPI.ClientCommands;
using Engine.Containers;
using Engine.Helpers;
using Engine.Model.Server;
using Engine.Network.Connections;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
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

      public string RequestId { get; set; }
      public string SenderId { get; set; }
    }
    #endregion

    #region fields
    Dictionary<string, ClientDescription> clientsEndPoints;
    List<RequestPair> requests;
    SynchronizationContext context;
    NetServer server;
    bool disposed;
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

      NetPeerConfiguration config = new NetPeerConfiguration(AsyncPeer.NetConfigString);
      config.MaximumConnections = 100;
      config.Port = port;

      if (usingIPv6)
        config.LocalAddress = IPAddress.IPv6Any;

      context = new SynchronizationContext();

      if (SynchronizationContext.Current == null)
        SynchronizationContext.SetSynchronizationContext(context);

      server = new NetServer(config);
      server.RegisterReceivedCallback(DataReceivedCallback);
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

      lock (requests)
        requests.Add(new RequestPair(requestId, senderId));

      AddressFamily addressFamily = ServerModel.Server.UsingIPv6 
        ? AddressFamily.InterNetworkV6 
        : AddressFamily.InterNetwork;

      if (!clientsEndPoints.ContainsKey(senderId))
        ServerModel.API.SendP2PConnectRequest(senderId, Port);

      if (!clientsEndPoints.ContainsKey(requestId))
        ServerModel.API.SendP2PConnectRequest(requestId, Port);
    }

    internal void RemoveEndPoint(string id)
    {
      lock (clientsEndPoints)
        clientsEndPoints.Remove(id);
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
            ServerModel.Logger.Write(new NetException(message.ReadString()));
            break;

          case NetIncomingMessageType.StatusChanged:
            NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

            if (status != NetConnectionStatus.Connected)
              break;

            NetIncomingMessage hailMessage = message.SenderConnection.RemoteHailMessage;

            string id = hailMessage.ReadString();
            IPEndPoint localPoint = hailMessage.ReadIPEndPoint();
            IPEndPoint publicPoint = message.SenderEndPoint;

            lock(clientsEndPoints)
              clientsEndPoints.Add(id, new ClientDescription(localPoint, publicPoint));

            TryDoneAllRequest();
            break;
        }
      }
    }

    private bool TryGetRequest(string senderId, string requestId, out IPEndPoint senderPoint, out IPEndPoint requestPoint)
    {
      bool received = true;

      ClientDescription senderDescription;
      ClientDescription requestDescription;

      lock (clientsEndPoints)
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
        lock(requests)
          requests.RemoveAll(p => string.Equals(p.SenderId, senderId) && string.Equals(p.RequestId, requestId));

        ServerModel.API.IntroduceConnections(senderId, senderEndPoint, requestId, requestEndPoint);
      }

      return received;
    }

    private void TryDoneAllRequest()
    {
      lock (requests)
        for (int i = requests.Count - 1; i >= 0; i--)
        {
          IPEndPoint senderEndPoint;
          IPEndPoint requestEndPoint;

          if (TryGetRequest(requests[i].SenderId, requests[i].RequestId, out senderEndPoint, out requestEndPoint))
          {
            ServerModel.API.IntroduceConnections(requests[i].SenderId, senderEndPoint, requests[i].RequestId, requestEndPoint);
            requests.RemoveAt(i);
          }
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

      lock (requests)
        requests.Clear();
    }
    #endregion
  }
}
