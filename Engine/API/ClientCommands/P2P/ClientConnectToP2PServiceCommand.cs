using Engine.Model.Client;
using System;
using System.Net;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientConnectToP2PServiceCommand :
    ClientCommand<ClientConnectToP2PServiceCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.ConnectToP2PService;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public override void Run(MessageContent content, ClientCommandArgs args)
    {
      if (args.PeerConnectionId != null)
        return;

      var address = ClientModel.Client.RemotePoint.Address;
      var endPoint = new IPEndPoint(address, content.Port);

      ClientModel.Peer.ConnectToService(endPoint);
    }

    [Serializable]
    public class MessageContent
    {
      private int port;

      public int Port
      {
        get { return port; }
        set { port = value; }
      }
    }
  }
}
