using System;
using System.Threading;

namespace Engine.Helpers
{
  public struct TryLock : IDisposable
  {
    private volatile bool lockTaken;
    private readonly object syncObject;

    public TryLock(object obj, int timeout = 10000)
    {
      syncObject = obj;
      lockTaken = Monitor.TryEnter(syncObject, timeout);
      if (!lockTaken)
        throw new InvalidOperationException("TryEnterLock timeout");
    }

    public void Dispose()
    {
      if (lockTaken)
        Monitor.Exit(syncObject);
    }
  }
}
