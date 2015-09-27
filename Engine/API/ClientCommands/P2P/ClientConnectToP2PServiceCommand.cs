using Engine.Helpers;
using Engine.Model.Client;
using System;
using System.Net;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientConnectToP2PServiceCommand :
    ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.ConnectToP2PService;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ClientCommandArgs args)
    {
      if (args.PeerConnectionId != null)
        return;

      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      var address = ClientModel.Client.RemotePoint.Address;
      var endPoint = new IPEndPoint(address, receivedContent.Port);

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
