using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{
  public interface ICommand<in TArgs>
  {
    void Run(TArgs args);
  }

  [Serializable]
  public class ServerCommandArgs
  {
    public string ConnectionId { get; set; }
    public byte[] Message { get; set; }
  }

  [Serializable]
  public class ClientCommandArgs
  {
    public string PeerConnectionId { get; set; }
    public byte[] Message { get; set; }
  }
}
