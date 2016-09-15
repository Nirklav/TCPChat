using Engine.Api.Client;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.Api.Server.P2P
{
  [Serializable]
  public class ServerSendP2PConnectRequestAction : IAction
  {
    private readonly string _nick;
    private readonly int _servicePort;

    /// <summary>
    /// Send request to user for connection to P2PService.
    /// </summary>
    /// <param name="nick">User who recive connection request.</param>
    /// <param name="servicePort">P2PService port.</param>
    [SecuritySafeCritical]
    public ServerSendP2PConnectRequestAction(string nick, int servicePort)
    {
      _nick = nick;
      _servicePort = servicePort;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var sendingContent = new ClientConnectToP2PServiceCommand.MessageContent { Port = _servicePort };
      ServerModel.Server.SendMessage(_nick, ClientConnectToP2PServiceCommand.CommandId, sendingContent);
    }
  }
}
