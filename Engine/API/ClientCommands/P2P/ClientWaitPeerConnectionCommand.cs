using Engine.API.ServerCommands;
using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Net;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientWaitPeerConnectionCommand :
      ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.WaitPeerConnection;

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

      if (receivedContent.RequestPoint == null)
        throw new ArgumentNullException("request point");

      if (receivedContent.SenderPoint == null)
        throw new ArgumentNullException("sender point");

      ClientModel.Peer.WaitConnection(receivedContent.SenderPoint);

      var sendingContent = new ServerP2PReadyAcceptCommand.MessageContent
      {
        PeerPoint = receivedContent.RequestPoint,
        ReceiverNick = receivedContent.RemoteInfo.Nick
      };

      using (var client = ClientModel.Get())
        sendingContent.RemoteInfo = client.User;

      ClientModel.Client.SendMessage(ServerP2PReadyAcceptCommand.CommandId, sendingContent);
    }

    [Serializable]
    public class MessageContent
    {
      private User remoteInfo;
      private IPEndPoint senderPoint;
      private IPEndPoint requestPoint;

      public User RemoteInfo
      {
        get { return remoteInfo; }
        set { remoteInfo = value; }
      }

      public IPEndPoint SenderPoint
      {
        get { return senderPoint; }
        set { senderPoint = value; }
      }

      public IPEndPoint RequestPoint
      {
        get { return requestPoint; }
        set { requestPoint = value; }
      }
    }
  }
}
