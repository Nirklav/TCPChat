using Engine.Model.Client;
using Engine.Network;
using System;
using System.Net;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientConnectToP2PServiceCommand :
      BaseCommand,
      IClientCommand
  {
    public void Run(ClientCommandArgs args)
    {
      if (args.PeerConnectionId != null)
        return;

      MessageContent receivedContent = GetContentFromMessage<MessageContent>(args.Message);

      IPAddress address = ClientModel.Client.RemotePoint.Address;
      IPEndPoint endPoint = new IPEndPoint(address, receivedContent.Port);

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
