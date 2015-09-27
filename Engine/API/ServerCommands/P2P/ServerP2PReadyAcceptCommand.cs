using Engine.API.ClientCommands;
using Engine.Helpers;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Net;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerP2PReadyAcceptCommand :
    BaseServerCommand,
    ICommand<ServerCommandArgs>
  {
    public const ushort CommandId = (ushort)ServerCommand.P2PReadyAccept;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ServerCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (receivedContent.RemoteInfo == null)
        throw new ArgumentNullException("Info");

      if (receivedContent.PeerPoint == null)
        throw new ArgumentNullException("PeerPoint");

      if (string.IsNullOrEmpty(receivedContent.ReceiverNick))
        throw new ArgumentException("receiverNick");

      if (!ServerModel.Server.ContainsConnection(receivedContent.ReceiverNick))
      {
        ServerModel.API.SendSystemMessage(args.ConnectionId, "Данного пользователя не существует.");
        return;
      }

      var connectContent = new ClientConnectToPeerCommand.MessageContent
      {
        PeerPoint = receivedContent.PeerPoint,
        RemoteInfo = receivedContent.RemoteInfo,
      };
      ServerModel.Server.SendMessage(receivedContent.ReceiverNick, ClientConnectToPeerCommand.CommandId, connectContent);
    }

    [Serializable]
    public class MessageContent
    {
      private string receiverNick;
      private IPEndPoint peerPoint;
      private User remoteInfo;

      public string ReceiverNick
      {
        get { return receiverNick; }
        set { receiverNick = value; }
      }

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
