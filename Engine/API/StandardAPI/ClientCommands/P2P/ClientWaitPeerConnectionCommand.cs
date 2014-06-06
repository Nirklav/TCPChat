using Engine.API.StandardAPI.ServerCommands;
using Engine.Connections;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Net;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientWaitPeerConnectionCommand :
      BaseCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.RemoteInfo == null)
        throw new ArgumentNullException("info");

      if (receivedContent.RequestPoint == null)
        throw new ArgumentNullException("request point");

      if (receivedContent.SenderPoint == null)
        throw new ArgumentNullException("sender point");

      string connectionId = PeerConnection.FromServiceConnectId(receivedContent.ServiceConnectId);
      ClientModel.Client.RegisterAndWait(connectionId, receivedContent.RemoteInfo.Nick, receivedContent.SenderPoint);

      using (var client = ClientModel.Get())
      {
        var sendingContent = new ServerP2PConnectResponceCommand.MessageContent
        {
          PeerPoint = receivedContent.RequestPoint,
          ReceiverNick = receivedContent.RemoteInfo.Nick,
          RemoteInfo = client.User,
          ServiceConnectId = receivedContent.ServiceConnectId,
        };

        ClientModel.Client.SendMessage(ServerP2PConnectResponceCommand.Id, sendingContent);
      }
    }

    [Serializable]
    public class MessageContent
    {
      User remoteInfo;
      IPEndPoint senderPoint;
      IPEndPoint requestPoint;
      int serviceConnectId;

      public User RemoteInfo { get { return remoteInfo; } set { remoteInfo = value; } }
      public IPEndPoint SenderPoint { get { return senderPoint; } set { senderPoint = value; } }
      public IPEndPoint RequestPoint { get { return requestPoint; } set { requestPoint = value; } }
      public int ServiceConnectId { get { return serviceConnectId; } set { serviceConnectId = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.WaitPeerConnection;
  }
}
