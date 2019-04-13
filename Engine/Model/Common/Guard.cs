using System;
using System.Security;
using System.Threading;

namespace Engine.Model
{
  public abstract class Guard<T> : MarshalByRefObject, IDisposable
    where T : class
  {
#if DEBUG
    private const int TimeOut = -1;
#else
    private const int TimeOut = 10000;
#endif

    [ThreadStatic] protected static Guard<T> _current;
    protected T _obj;

    [SecurityCritical]
    protected Guard(T obj)
    {
      if (!Monitor.TryEnter(obj, TimeOut))
        throw new InvalidOperationException("model lock timeout");

      _obj = obj;

      if (_current == null)
        _current = this;
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_current == this)
        _current = null;

      var obj = _obj;
      _obj = default;

      Monitor.Exit(obj);
    }
  }
}
