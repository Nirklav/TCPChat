using Engine.Model.Client;
using Engine.Network;
using System;
using System.Net;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientConnectToP2PServiceCommand :
      BaseCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      if (args.PeerConnectionId != null)
        return;

      MessageContent receivedContent = GetContentFromMessage<MessageContent>(args.Message);

      if (receivedContent.ServicePoint == null)
        throw new ArgumentNullException("ServicePoint");

      ClientModel.Peer.ConnectToService(receivedContent.ServicePoint);
    }

    [Serializable]
    public class MessageContent
    {
      IPEndPoint servicePoint;

      public IPEndPoint ServicePoint { get { return servicePoint; } set { servicePoint = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.ConnectToP2PService;
  }
}
