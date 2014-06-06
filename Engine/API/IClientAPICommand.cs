using Engine.Network.Connections;

namespace Engine
{
  public interface IClientAPICommand
  {
    void Run(ClientCommandArgs args);
  }

  public class ClientCommandArgs
  {
    public string PeerConnectionId { get; set; }
    public byte[] Message { get; set; }
  }
}
