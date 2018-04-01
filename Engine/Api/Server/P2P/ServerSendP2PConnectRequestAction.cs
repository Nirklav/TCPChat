using Engine.Api.Client.P2P;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.Api.Server.P2P
{
  [Serializable]
  public class ServerSendP2PConnectRequestAction : IAction
  {
    private readonly UserId _userId;
    private readonly int _servicePort;

    /// <summary>
    /// Send request to user for connection to P2PService.
    /// </summary>
    /// <param name="userId">User who recive connection request.</param>
    /// <param name="servicePort">P2PService port.</param>
    [SecuritySafeCritical]
    public ServerSendP2PConnectRequestAction(UserId userId, int servicePort)
    {
      _userId = userId;
      _servicePort = servicePort;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var sendingContent = new ClientConnectToP2PServiceCommand.MessageContent { Port = _servicePort };
      ServerModel.Server.SendMessage(_userId, ClientConnectToP2PServiceCommand.CommandId, sendingContent);
    }
  }
}
