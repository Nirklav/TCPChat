using System.Net;

namespace Engine
{
  public interface IServerAPI
  {
    string Name { get; }

    ICommand<ServerCommandArgs> GetCommand(byte[] message);

    void SendP2PConnectRequest(string nick, int port);
    void IntroduceConnections(string senderId, IPEndPoint senderPoint, string requestId, IPEndPoint requestPoint);
    void SendSystemMessage(string nick, string message);
    void RemoveUser(string nick);
  }
}
