using System;
using System.Collections.Generic;
using System.Threading;

namespace Engine.Network
{
  public class PackageTracker : IDisposable
  {
    private const int Timeout = 30000;

    private struct TrackedPackage
    {
      public readonly IPackage Package;
      public readonly DateTime AddedAt;

      public TrackedPackage(IPackage package, DateTime addedAt)
      {
        Package = package;
        AddedAt = addedAt;
      }
    }

    private readonly object _syncObject = new object();
    private readonly Dictionary<long, TrackedPackage> _packages;
    private long _nextId;

    private Timer _checkTimer;

    public event EventHandler<PackageTrackerEventArgs> Delivered;
    public event EventHandler<PackageTrackerEventArgs> Error;

    public PackageTracker()
    {
      _packages = new Dictionary<long, TrackedPackage>();
    }

    public void Track(IPackage package)
    {
      lock (_syncObject)
      {
        package.TrackedId = _nextId++;
        _packages.Add(package.TrackedId, new TrackedPackage(package, DateTime.UtcNow));
      }
    }

    public void Dispose()
    {
      lock (_syncObject)
      {
        throw new NotImplementedException();
      }
    }
  }
}
