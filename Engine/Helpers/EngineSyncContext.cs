using System.Collections.Generic;
using System.Security;
using System.Threading;

namespace Engine.Helpers
{
  [SecurityCritical]
  class EngineSyncContext : SynchronizationContext
  {
    [SecurityCritical]
    private class Event
    {
      private SendOrPostCallback _callback;
      private object _state;
      private ManualResetEvent _resetEvent;

      public WaitHandle Handle
      {
        [SecurityCritical]
        get { return _resetEvent; }
      }

      [SecurityCritical]
      public Event(SendOrPostCallback callback, object state, bool isSend)
      {
        _callback = callback;
        _state = state;

        _resetEvent = new ManualResetEvent(!isSend);
      }

      [SecurityCritical]
      public void Dispatch()
      {
        var e = _callback;
        if (e != null)
          e(_state);

        _resetEvent.Set();
      }
    }

    private object _syncObject = new object();
    private volatile bool _inProcess;
    private Queue<Event> _callbackQueue;

    [SecurityCritical]
    public EngineSyncContext()
    {
      _callbackQueue = new Queue<Event>();
    }

    [SecuritySafeCritical]
    public override void Post(SendOrPostCallback d, object state)
    {
      lock (_syncObject)
      {
        _callbackQueue.Enqueue(new Event(d, state, false));

        StartThread();
      }
    }

    [SecuritySafeCritical]
    public override void Send(SendOrPostCallback callback, object state)
    {
      Event item;
      lock (_syncObject)
      {
        item = new Event(callback, state, true);
        _callbackQueue.Enqueue(item);

        StartThread();
      }

      item.Handle.WaitOne();
    }

    [SecuritySafeCritical]
    public override SynchronizationContext CreateCopy()
    {
      return this;
    }

    [SecurityCritical]
    private void StartThread()
    {
      if (_inProcess)
        return;

      _inProcess = true;
      ThreadPool.QueueUserWorkItem(ThreadFunc);
    }

    [SecurityCritical]
    private void ThreadFunc(object state)
    {
      var oldSyncContext = Current;
      SetSynchronizationContext(this);

      while (true)
      {
        Event e;
        lock (_syncObject)
        {
          if (_callbackQueue.Count <= 0)
          {
            SetSynchronizationContext(oldSyncContext);
            _inProcess = false;           
            return;
          }

          e = _callbackQueue.Dequeue();         
        }

        e.Dispatch();
      }
    }
  }
}
