using System;
using System.Collections.Generic;
using System.Net;
using Engine.Concrete.Connections;
using Engine.Concrete.Entities;

namespace Engine.Concrete
{
  public class AddressReceivedEventArgs : EventArgs
  {
    public IPEndPoint RemoutePoint { get; set; }
    public ServerConnection ReceivingConnection { get; set; }
    public ServerConnection SendedConnection { get; set; }
    public Exception Error { get; set; }
  }
}
