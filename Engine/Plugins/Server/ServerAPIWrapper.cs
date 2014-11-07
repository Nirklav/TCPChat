using Engine.Model.Server;
using System;
using System.Net;

namespace Engine.Plugins.Server
{
  public class ServerAPIWrapper :
    MarshalByRefObject,
    IServerAPI
  {
    public string Name { get { return ServerModel.API.Name; } }
    public IServerCommand GetCommand(byte[] message) { return ServerModel.API.GetCommand(message); }
    public void SendP2PConnectRequest(string nick, int port) { ServerModel.API.SendP2PConnectRequest(nick, port); }
    public void IntroduceConnections(string senderId, IPEndPoint senderPoint, string requestId, IPEndPoint requestPoint) { ServerModel.API.IntroduceConnections(senderId, senderPoint, requestId, requestPoint); }
    public void SendSystemMessage(string nick, string message) { ServerModel.API.SendSystemMessage(nick, message); }
    public void RemoveUser(string nick) { ServerModel.API.RemoveUser(nick); }

    public bool IsInited { get { return ServerModel.API != null; } }
  }
}
