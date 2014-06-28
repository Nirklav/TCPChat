using Engine.API.StandardAPI.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network.Connections;
using System;
using System.Net;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerP2PConnectResponceCommand :
      BaseServerCommand,
      IServerAPICommand
  {
    public void Run(ServerCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

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
      ServerModel.Server.SendMessage(receivedContent.ReceiverNick, ClientConnectToPeerCommand.Id, connectContent);
    }

    [Serializable]
    public class MessageContent
    {
      string receiverNick;
      IPEndPoint peerPoint;
      User remoteInfo;

      public string ReceiverNick { get { return receiverNick; } set { receiverNick = value; } }
      public IPEndPoint PeerPoint { get { return peerPoint; } set { peerPoint = value; } }
      public User RemoteInfo { get { return remoteInfo; } set { remoteInfo = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.P2PConnectResponce;
  }
}
