using Engine.Network.Connections;
using System;

namespace Engine.API
{
  public interface ICommand
  {
    long Id { get; }
  }

  public interface ICommand<in TArgs> : ICommand
    where TArgs : CommandArgs
  {
    void Run(TArgs args);
  }

  [Serializable]
  public class CommandArgs
  {
    public IPackage Package { get; private set; }

    public CommandArgs(IPackage package)
    {
      Package = package;
    }
  }

  [Serializable]
  public class ServerCommandArgs 
    : CommandArgs
  {
    public string ConnectionId { get; private set; }
    
    public ServerCommandArgs(string connectionId, IPackage package)
      : base(package)
    {
      ConnectionId = connectionId;
    }
  }

  [Serializable]
  public class ClientCommandArgs 
    : CommandArgs
  {
    public string PeerConnectionId { get; private set; }

    public ClientCommandArgs(string peerConnectionId, IPackage package)
      : base(package)
    {
      PeerConnectionId = peerConnectionId;
    }
  }
}