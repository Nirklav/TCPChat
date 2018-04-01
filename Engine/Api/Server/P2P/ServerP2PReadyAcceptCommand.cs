using System;
using System.Security;
using Engine.Api.Client.P2P;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Server.P2P
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
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (content.RemoteInfo == null)
        throw new ArgumentNullException("content.RemoteInfo");

      if (!ServerModel.Server.ContainsConnection(content.ReceiverId))
      {
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.P2PUserNotExist));
        return;
      }

      var connectContent = new ClientConnectToPeerCommand.MessageContent
      {
        Port = content.PeerPort,
        IPAddress = content.PeerIPAddress,
        RemoteInfo = content.RemoteInfo
      };

      ServerModel.Server.SendMessage(content.ReceiverId, ClientConnectToPeerCommand.CommandId, connectContent);
    }

    [Serializable]
    [BinType("ServerP2PReadyAccept")]
    public class MessageContent
    {
      [BinField("r")]
      public UserId ReceiverId;

      [BinField("p")]
      public int PeerPort;

      [BinField("a")]
      public byte[] PeerIPAddress;

      [BinField("u")]
      public UserDto RemoteInfo;
    }
  }
}
