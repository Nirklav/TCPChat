using Engine.Model.Client;
using Engine.Model.Common.Dto;
using System;
using System.Net;
using System.Security;

namespace Engine.Api.Client
{
  [SecurityCritical]
  class ClientConnectToPeerCommand :
    ClientCommand<ClientConnectToPeerCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.ConnectToPeer;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (content.RemoteInfo == null)
        throw new ArgumentNullException("info");

      if (content.PeerPoint == null)
        throw new ArgumentNullException("PeerPoint");

      ClientModel.Peer.ConnectToPeer(content.RemoteInfo.Nick, content.PeerPoint);
    }

    [Serializable]
    public class MessageContent
    {
      private IPEndPoint _peerPoint;
      private UserDto _remoteInfo;

      public IPEndPoint PeerPoint
      {
        get { return _peerPoint; }
        set { _peerPoint = value; }
      }

      public UserDto RemoteInfo
      {
        get { return _remoteInfo; }
        set { _remoteInfo = value; }
      }
    }
  }
}
