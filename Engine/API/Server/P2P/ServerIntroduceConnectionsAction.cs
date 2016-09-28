using Engine.Api.Client;
using Engine.Model.Common.Dto;
using Engine.Model.Server;
using System;
using System.Net;
using System.Security;

namespace Engine.Api.Server.P2P
{
  [Serializable]
  public class ServerIntroduceConnectionsAction : IAction
  {
    private readonly string _senderId;
    private readonly IPEndPoint _senderPoint;
    private readonly string _requestId;
    private readonly IPEndPoint _requestPoint;

    /// <summary>
    /// Connect users directly.
    /// </summary>
    /// <param name="senderId">User who request direct connection.</param>
    /// <param name="senderPoint">Address of user who request direct connection.</param>
    /// <param name="requestId">Requested user.</param>
    /// <param name="requestPoint">Address of requested user.</param>
    [SecuritySafeCritical]
    public ServerIntroduceConnectionsAction(string senderId, IPEndPoint senderPoint, string requestId, IPEndPoint requestPoint)
    {
      _senderId = senderId;
      _senderPoint = senderPoint;
      _requestId = requestId;
      _requestPoint = requestPoint;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      using (var server = ServerModel.Get())
      {
        var senderUser = server.Chat.GetUser(_senderId);
        var content = new ClientWaitPeerConnectionCommand.MessageContent
        {
          RequestIPAddress = _requestPoint.Address.GetAddressBytes(),
          RequestPort = _requestPoint.Port,
          SenderIPAddress = _senderPoint.Address.GetAddressBytes(),
          SenderPort = _senderPoint.Port,
          RemoteInfo = new UserDto(senderUser),
        };

        ServerModel.Server.SendMessage(_requestId, ClientWaitPeerConnectionCommand.CommandId, content);
      }
    }
  }
}
