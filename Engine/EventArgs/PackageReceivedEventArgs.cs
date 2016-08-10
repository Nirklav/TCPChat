using Engine.Network;
using Engine.Network.Connections;
using System;

namespace Engine
{
  [Serializable]
  public class PackageReceivedEventArgs : EventArgs
  {
    public Unpacked<IPackage> Unpacked { get; private set; }
    public Exception Exception { get; private set; }

    public PackageReceivedEventArgs(Unpacked<IPackage> unpacked)
    {
      Unpacked = unpacked;
    }

    public PackageReceivedEventArgs(Exception e)
    {
      Exception = e;
    }
  }
}
