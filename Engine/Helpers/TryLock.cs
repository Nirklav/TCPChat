using System;
using System.Threading;

namespace Engine.Helpers
{
  public struct TryLock : IDisposable
  {
    private volatile bool _lockTaken;
    private readonly object _syncObject;

    public TryLock(object obj, int timeout = 10000)
    {
      _syncObject = obj;
      _lockTaken = Monitor.TryEnter(_syncObject, timeout);
      if (!_lockTaken)
        throw new InvalidOperationException("TryEnterLock timeout");
    }

    public void Dispose()
    {
      if (_lockTaken)
        Monitor.Exit(_syncObject);
    }
  }
}
