using Engine.Network.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{
  public interface IServerCommand
  {
    void Run(ServerCommandArgs args);
  }

  [Serializable]
  public class ServerCommandArgs
  {
    public string ConnectionId { get; set; }
    public byte[] Message { get; set; }
  }
}
