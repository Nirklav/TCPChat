using Engine.API.StandardAPI.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network;
using Engine.Network.Connections;
using System;
using System.Net;
using System.Net.Sockets;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerP2PConnectRequestCommand :
      BaseServerCommand,
      IServerAPICommand
  {
    public void Run(ServerCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.Nick == null)
        throw new ArgumentNullException("Info");

      if (!ServerModel.Server.ContainsConnection(receivedContent.Nick))
      {
        ServerModel.API.SendSystemMessage(args.ConnectionId, "Данного пользователя не существует.");
        return;
      }

      int id = ServerModel.Server.P2PService.WaitConnection(receivedContent.Nick, args.ConnectionId);

      IPAddress serviceAddress = Connection.GetIPAddress(ServerModel.Server.UsingIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork);
      var sendingContent = new ClientConnectToP2PServiceCommand.MessageContent
      {
        ServicePoint = new IPEndPoint(serviceAddress, ServerModel.Server.P2PService.Port),
        ServiceConnectId = id,
        Type = ConnectionType.Request,
      };
      ServerModel.Server.SendMessage(receivedContent.Nick, ClientConnectToP2PServiceCommand.Id, sendingContent);

      sendingContent.Type = ConnectionType.Sender;
      ServerModel.Server.SendMessage(args.ConnectionId, ClientConnectToP2PServiceCommand.Id, sendingContent);
    }

    [Serializable]
    public class MessageContent
    {
      string nick;

      public string Nick { get { return nick; } set { nick = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.P2PConnectRequest;
  }
}
