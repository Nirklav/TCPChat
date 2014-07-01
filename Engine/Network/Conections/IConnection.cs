using Engine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Network.Connections
{
  public interface IConnection : IDisposable
  {
    string Id { get; set; }
    void SendMessage(ushort id, object messageContent);
  }
}
