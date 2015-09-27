using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Net;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientConnectToPeerCommand :
    ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.ConnectToPeer;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
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
      private IPEndPoint peerPoint;
      private User remoteInfo;

      public IPEndPoint PeerPoint
      {
        get { return peerPoint; }
        set { peerPoint = value; }
      }

      public User RemoteInfo
      {
        get { return remoteInfo; }
        set { remoteInfo = value; }
      }
    }
  }
}
