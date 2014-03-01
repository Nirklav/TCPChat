using Engine.Concrete;
using Engine.Concrete.Connections;
using Engine.Concrete.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Abstract
{
  public interface IServerAPI
  {
    string APIName { get; }

    IServerAPICommand GetCommand(byte[] message);
    AsyncServer Server { get; }

    void IntroduceConnections(ConnectionsContainer container);
    void SendSystemMessage(ServerConnection receiveConnection, string message);
    void CloseConnection(string nick);
    void CloseConnection(ServerConnection connection);
  }
}
