using System;
using System.Net;
using System.Security;
using Engine.Api.Server.P2P;
using Engine.Model.Client;
using Engine.Model.Common.Dto;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.P2P
{
  [SecurityCritical]
  class ClientWaitPeerConnectionCommand :
    ClientCommand<ClientWaitPeerConnectionCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.WaitPeerConnection;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (content.RemoteInfo == null)
        throw new ArgumentNullException("content.RemoteInfo");

      var senderPoint = new IPEndPoint(new IPAddress(content.SenderIpAddress), content.SenderPort);
      ClientModel.Peer.WaitConnection(senderPoint);

      var sendingContent = new ServerP2PReadyAcceptCommand.MessageContent
      {
        PeerPort = content.RequestPort,
        PeerIPAddress = content.RequestIpAddress,
        ReceiverNick = content.RemoteInfo.Nick
      };

      using (var client = ClientModel.Get())
        sendingContent.RemoteInfo = new UserDto(client.Chat.User);

      ClientModel.Client.SendMessage(ServerP2PReadyAcceptCommand.CommandId, sendingContent);
      ClientModel.Logger.WriteDebug("ClientWaitPeerConnectionCommand: {0}|{1}|{2}|{3}", sendingContent.PeerIPAddress, sendingContent.PeerPort, sendingContent.ReceiverNick, sendingContent.RemoteInfo.Nick);
    }

    [Serializable]
    [BinType("ClientWaitPeerConnection")]
    public class MessageContent
    {
      [BinField("u")]
      public UserDto RemoteInfo;

      [BinField("sp")]
      public int SenderPort;

      [BinField("sa")]
      public byte[] SenderIpAddress;

      [BinField("rp")]
      public int RequestPort;

      [BinField("ra")]
      public byte[] RequestIpAddress;
    }
  }
}
