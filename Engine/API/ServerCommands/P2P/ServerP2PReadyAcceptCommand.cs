using Engine.Api.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Net;
using System.Security;

namespace Engine.Api.ServerCommands
{
  [SecurityCritical]
  class ServerP2PReadyAcceptCommand :
    ServerCommand<ServerP2PReadyAcceptCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.P2PReadyAccept;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ServerCommandArgs args)
    {
      if (content.RemoteInfo == null)
        throw new ArgumentNullException("Info");

      if (content.PeerPoint == null)
        throw new ArgumentNullException("PeerPoint");

      if (string.IsNullOrEmpty(content.ReceiverNick))
        throw new ArgumentException("receiverNick");

      if (!ServerModel.Server.ContainsConnection(content.ReceiverNick))
      {
        ServerModel.Api.SendSystemMessage(args.ConnectionId, SystemMessageId.P2PUserNotExist);
        return;
      }

      var connectContent = new ClientConnectToPeerCommand.MessageContent
      {
        PeerPoint = content.PeerPoint,
        RemoteInfo = content.RemoteInfo,
      };

      ServerModel.Server.SendMessage(content.ReceiverNick, ClientConnectToPeerCommand.CommandId, connectContent);
    }

    [Serializable]
    public class MessageContent
    {
      private string _receiverNick;
      private IPEndPoint _peerPoint;
      private User _remoteInfo;

      public string ReceiverNick
      {
        get { return _receiverNick; }
        set { _receiverNick = value; }
      }

      public IPEndPoint PeerPoint
      {
        get { return _peerPoint; }
        set { _peerPoint = value; }
      }

      public User RemoteInfo
      {
        get { return _remoteInfo; }
        set { _remoteInfo = value; }
      }
    }
  }
}
