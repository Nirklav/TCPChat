using Engine.Containers;
using Engine.Network;
using Engine.Network.Connections;
using System;
using System.Net;

namespace Engine
{
  public interface IServerAPI
  {
    string Name { get; }

    IServerAPICommand GetCommand(byte[] message);

    void SendP2PConnectRequest(string nick, int port);
    void IntroduceConnections(string senderId, IPEndPoint senderPoint, string requestId, IPEndPoint requestPoint);
    void SendSystemMessage(string nick, string message);
    void RemoveUser(string nick);
  }
}
