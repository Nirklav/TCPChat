using Engine.Connections;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Net;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientConnectToPeerCommand :
      BaseCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.RemoteInfo == null)
        throw new ArgumentNullException("info");

      if (receivedContent.PeerPoint == null)
        throw new ArgumentNullException("PeerPoint");

      string connectionId = PeerConnection.FromServiceConnectId(receivedContent.ServiceConnectId);
      ClientModel.Client.RegisterAndConnect(connectionId, receivedContent.RemoteInfo.Nick, receivedContent.PeerPoint);
    }

    [Serializable]
    public class MessageContent
    {
      int serviceConnectId;
      IPEndPoint peerPoint;
      User remoteInfo;

      public int ServiceConnectId { get { return serviceConnectId; } set { serviceConnectId = value; } }
      public IPEndPoint PeerPoint { get { return peerPoint; } set { peerPoint = value; } }
      public User RemoteInfo { get { return remoteInfo; } set { remoteInfo = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.ConnectToPeer;
  }
}
