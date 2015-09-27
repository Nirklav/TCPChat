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
      private SendOrPostCallback callback;
      private object state;
      private ManualResetEvent resetEvent;

      public WaitHandle Handle
      {
        [SecurityCritical]
        get { return resetEvent; }
      }

      [SecurityCritical]
      public Event(SendOrPostCallback callback, object state, bool isSend)
      {
        this.callback = callback;
        this.state = state;

        resetEvent = new ManualResetEvent(!isSend);
      }

      [SecurityCritical]
      public void Dispatch()
      {
        var e = callback;
        if (e != null)
          e(state);

        resetEvent.Set();
      }
    }

    private object syncObject = new object();
    private volatile bool inProcess;
    private Queue<Event> callbackQueue;

    [SecurityCritical]
    public EngineSyncContext()
    {
      callbackQueue = new Queue<Event>();
    }

    [SecuritySafeCritical]
    public override void Post(SendOrPostCallback d, object state)
    {
      lock (syncObject)
      {
        callbackQueue.Enqueue(new Event(d, state, false));

        StartThread();
      }
    }

    [SecuritySafeCritical]
    public override void Send(SendOrPostCallback d, object state)
    {
      Event item;
      lock (syncObject)
      {
        item = new Event(d, state, true);
        callbackQueue.Enqueue(item);

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
      if (inProcess)
        return;

      inProcess = true;
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
        lock (syncObject)
        {
          if (callbackQueue.Count <= 0)
          {
            SetSynchronizationContext(oldSyncContext);
            inProcess = false;           
            return;
          }

          e = callbackQueue.Dequeue();         
        }

        e.Dispatch();
      }
    }
  }
}
