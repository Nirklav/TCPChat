using Engine.Network.Connections;
using System;

namespace Engine
{
  [Serializable]
  public class PackageReceivedEventArgs : EventArgs
  {
    public IPackage Package { get; private set; }
    public Exception Exception { get; private set; }

    public PackageReceivedEventArgs(IPackage package)
    {
      Package = package;
    }

    public PackageReceivedEventArgs(Exception e)
    {
      Exception = e;
    }
  }
}
