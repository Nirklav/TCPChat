using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Engine.Helpers
{
  class EngineSyncContext : SynchronizationContext
  {
    private class Event
    {
      private SendOrPostCallback callback;
      private object state;
      private ManualResetEvent resetEvent;

      public WaitHandle Handle { get { return resetEvent; } }

      public Event(SendOrPostCallback callback, object state, bool isSend)
      {
        this.callback = callback;
        this.state = state;

        resetEvent = new ManualResetEvent(!isSend);
      }

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

    public EngineSyncContext()
    {
      callbackQueue = new Queue<Event>();
    }

    public override void Post(SendOrPostCallback d, object state)
    {
      lock (syncObject)
      {
        callbackQueue.Enqueue(new Event(d, state, false));

        StartThread();
      }
    }

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

    public override SynchronizationContext CreateCopy()
    {
      return this;
    }

    private void StartThread()
    {
      if (inProcess)
        return;

      inProcess = true;
      ThreadPool.QueueUserWorkItem(ThreadFunc);
    }

    private void ThreadFunc(object state)
    {
      SynchronizationContext.SetSynchronizationContext(this);

      while (true)
      {
        Event e;
        lock (syncObject)
        {
          if (callbackQueue.Count <= 0)
          {
            SynchronizationContext.SetSynchronizationContext(null);
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
