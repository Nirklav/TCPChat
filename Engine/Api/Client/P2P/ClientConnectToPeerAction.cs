using Engine.Api.Server.P2P;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Api.Client.P2P
{
  public class ClientConnectToPeerAction : IAction
  {
    private readonly UserId _userId;

    /// <summary>
    /// Initiate new direct connection to other user.
    /// If connection already exist then action do nothing.
    /// </summary>
    /// <param name="userId">User nick.</param>
    [SecuritySafeCritical]
    public ClientConnectToPeerAction(UserId userId)
    {
      if (userId == UserId.Empty)
        throw new ArgumentException(nameof(userId));

      _userId = userId;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      if (ClientModel.Peer.IsConnected(_userId))
      {
        ClientModel.Logger.WriteDebug("Client already connected to {0}", _userId);
      }
      else
      {
        var sendingContent = new ServerP2PConnectRequestCommand.MessageContent { UserId = _userId };
        ClientModel.Client.SendMessage(ServerP2PConnectRequestCommand.CommandId, sendingContent);
        ClientModel.Logger.WriteDebug("Connecting directly to {0}...", _userId);
      }
    }
  }
}
