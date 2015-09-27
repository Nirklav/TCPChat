using System;

namespace Engine.API
{
  public interface ICommand
  {
    ushort Id { get; }
  }

  public interface ICommand<in TArgs> : ICommand
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
