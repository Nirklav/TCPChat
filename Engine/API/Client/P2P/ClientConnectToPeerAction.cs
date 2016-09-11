using Engine.Api.Server;
using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.Api.Client.P2P
{
  public class ClientConnectToPeerAction : IAction
  {
    private readonly string _nick;

    /// <summary>
    /// Initiate new direct connection to other user.
    /// If connection already exist then action do nothing.
    /// </summary>
    /// <param name="nick">User nick.</param>
    [SecuritySafeCritical]
    public ClientConnectToPeerAction(string nick)
    {
      if (string.IsNullOrEmpty(nick))
        throw new ArgumentException("nick");

      _nick = nick;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      if (ClientModel.Peer.IsConnected(_nick))
        return;

      var sendingContent = new ServerP2PConnectRequestCommand.MessageContent { Nick = _nick };
      ClientModel.Client.SendMessage(ServerP2PConnectRequestCommand.CommandId, sendingContent);
    }
  }
}
