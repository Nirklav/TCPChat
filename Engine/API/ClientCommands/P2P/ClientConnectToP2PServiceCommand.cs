using Engine.Helpers;
using Engine.Model.Client;
using System;
using System.Net;

namespace Engine.API.ClientCommands
{
  class ClientConnectToP2PServiceCommand :
      ICommand<ClientCommandArgs>
  {
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
      int port;

      public int Port { get { return port; } set { port = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.ConnectToP2PService;
  }
}
