using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Net;

namespace Engine.API.ClientCommands
{
  class ClientConnectToPeerCommand :
      ICommand<ClientCommandArgs>
  {
    public void Run(ClientCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (receivedContent.RemoteInfo == null)
        throw new ArgumentNullException("info");

      if (receivedContent.PeerPoint == null)
        throw new ArgumentNullException("PeerPoint");

      ClientModel.Peer.ConnectToPeer(receivedContent.RemoteInfo.Nick, receivedContent.PeerPoint);
    }

    [Serializable]
    public class MessageContent
    {
      IPEndPoint peerPoint;
      User remoteInfo;

      public IPEndPoint PeerPoint { get { return peerPoint; } set { peerPoint = value; } }
      public User RemoteInfo { get { return remoteInfo; } set { remoteInfo = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.ConnectToPeer;
  }
}
