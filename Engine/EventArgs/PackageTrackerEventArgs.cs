using Engine.Network;
using System;

namespace Engine
{
  public class PackageTrackerEventArgs : EventArgs
  {
    public IPackage Package { get; private set; }

    public PackageTrackerEventArgs(IPackage package)
    {
      Package = package;
    }
  }
}
