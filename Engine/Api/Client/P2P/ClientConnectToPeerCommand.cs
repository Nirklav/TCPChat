using System;
using System.Net;
using System.Security;
using Engine.Model.Client;
using Engine.Model.Common.Dto;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.P2P
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
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (content.RemoteInfo == null)
        throw new ArgumentNullException("content.RemoteInfo");

      var peerPoint = new IPEndPoint(new IPAddress(content.IPAddress), content.Port);
      ClientModel.Peer.ConnectToPeer(content.RemoteInfo.Nick, peerPoint);
      ClientModel.Logger.WriteDebug("ClientConnectToPeerCommand: {0}|{1}", content.RemoteInfo.Nick, peerPoint.ToString());
    }

    [Serializable]
    [BinType("ClientConnectToPeer")]
    public class MessageContent
    {
      [BinField("p")]
      public int Port;

      [BinField("a")]
      public byte[] IPAddress;

      [BinField("u")]
      public UserDto RemoteInfo;
    }
  }
}
