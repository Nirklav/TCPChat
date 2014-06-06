using Engine.Containers;
using Engine.Network;
using Engine.Network.Connections;

namespace Engine
{
  public interface IServerAPI
  {
    string Name { get; }

    IServerAPICommand GetCommand(byte[] message);

    void IntroduceConnections(ConnectionsContainer container);
    void SendSystemMessage(string nick, string message);
    void CloseConnection(string nick);
  }
}
