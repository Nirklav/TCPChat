using Engine.Connections;
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
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.ServicePoint == null)
        throw new ArgumentNullException("ServicePoint");

      PeerConnection connection = ClientModel.Client.CreatePeerConnection();
      connection.ConnectToService(receivedContent.ServicePoint, receivedContent.ServiceConnectId, receivedContent.Type);
    }

    [Serializable]
    public class MessageContent
    {
      IPEndPoint servicePoint;
      ConnectionType type;
      int serviceConnectId;

      public IPEndPoint ServicePoint { get { return servicePoint; } set { servicePoint = value; } }
      public ConnectionType Type { get { return type; } set { type = value; } }
      public int ServiceConnectId { get { return serviceConnectId; } set { serviceConnectId = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.ConnectToP2PService;
  }
}
