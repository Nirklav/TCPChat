using Engine.Concrete.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Abstract
{
  public interface IServerAPICommand
  {
    void Run(ServerCommandArgs args);
  }

  public class ServerCommandArgs
  {
    public ServerConnection UserConnection { get; set; }
    public IServerAPI API { get; set; }
    public byte[] Message { get; set; }
  }
}
