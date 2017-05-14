using Engine.Network;
using System;
using System.Security;

namespace Engine.Api
{
  public interface ICommand
  {
    long Id { get; }
    void Run(CommandArgs args);
  }

  [Serializable]
  public class CommandArgs : IDisposable
  {
    public string ConnectionId { get; private set; }
    public Unpacked<IPackage> Unpacked { get; private set; }

    public CommandArgs(string connectionId, Unpacked<IPackage> unpacked)
    {
      ConnectionId = connectionId;
      Unpacked = unpacked;
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      Unpacked.Dispose();
    }
  }
}