using Engine.API;
using Engine.Model.Server;
using System.Net;
using System.Security;

namespace Engine.Plugins.Server
{
  [SecuritySafeCritical]
  public class ServerAPIWrapper :
    CrossDomainObject,
    IServerAPI
  {
    public bool IsInited
    {
      [SecuritySafeCritical]
      get { return ServerModel.API != null; }
    }

    public string Name
    {
      [SecuritySafeCritical]
      get { return ServerModel.API.Name; }
    }

    [SecuritySafeCritical]
    public ICommand<ServerCommandArgs> GetCommand(byte[] message)
    {
      return ServerModel.API.GetCommand(message);
    }

    [SecuritySafeCritical]
    public void SendP2PConnectRequest(string nick, int port)
    {
      ServerModel.API.SendP2PConnectRequest(nick, port);
    }

    [SecuritySafeCritical]
    public void IntroduceConnections(string senderId, IPEndPoint senderPoint, string requestId, IPEndPoint requestPoint)
    {
      ServerModel.API.IntroduceConnections(senderId, senderPoint, requestId, requestPoint);
    }

    [SecuritySafeCritical]
    public void SendSystemMessage(string nick, string message)
    {
      ServerModel.API.SendSystemMessage(nick, message);
    }

    [SecuritySafeCritical]
    public void RemoveUser(string nick)
    {
      ServerModel.API.RemoveUser(nick);
    }
  }
}
