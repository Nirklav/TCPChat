using Engine.Network.Connections;
using System;
using System.Net;

namespace Engine
{
  public class AddressReceivedEventArgs : EventArgs
  {
    public IPEndPoint RemoutePoint { get; set; }
    public ServerConnection ReceivingConnection { get; set; }
    public ServerConnection SendedConnection { get; set; }
    public Exception Error { get; set; }
  }
}
