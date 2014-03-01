using Engine.Concrete.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Abstract
{
  public interface IClientAPICommand
  {
    void Run(ClientCommandArgs args);
  }

  public class ClientCommandArgs
  {
    public PeerConnection Peer { get; set; }
    public IClientAPI API { get; set; }
    public byte[] Message { get; set; }
  }
}
