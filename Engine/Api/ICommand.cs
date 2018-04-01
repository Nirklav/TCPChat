using Engine.Model.Common.Entities;
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
  public sealed class CommandArgs : IDisposable
  {
    public UserId ConnectionId { get; private set; }
    public Unpacked<IPackage> Unpacked { get; private set; }

    public CommandArgs(UserId connectionId, Unpacked<IPackage> unpacked)
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