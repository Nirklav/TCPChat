using Engine.Network.Connections;
using System;

namespace Engine
{
  public interface IClientCommand
  {
    void Run(ClientCommandArgs args);
  }

  [Serializable]
  public class ClientCommandArgs
  {
    public string PeerConnectionId { get; set; }
    public byte[] Message { get; set; }
  }
}
