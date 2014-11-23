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
    private Queue<Event> callbackQueue;
    private Thread engineTread;

    public EngineSyncContext()
    {
      callbackQueue = new Queue<Event>();
    }

    public override void Post(SendOrPostCallback d, object state)
    {
      lock (syncObject)
        callbackQueue.Enqueue(new Event(d, state, false));

      StartThread();
    }

    public override void Send(SendOrPostCallback d, object state)
    {
      Event item;
      lock (syncObject)
      {
        item = new Event(d, state, true);
        callbackQueue.Enqueue(item);
      }

      StartThread();
      item.Handle.WaitOne();
    }

    public override SynchronizationContext CreateCopy()
    {
      return this;
    }

    private void StartThread()
    {
      if (engineTread != null && engineTread.IsAlive)
        return;

      engineTread = new Thread(ThreadFunc);
      engineTread.IsBackground = true;
      engineTread.Start();
    }

    private void ThreadFunc()
    {
      SynchronizationContext.SetSynchronizationContext(this);

      while (true)
      {
        Event e;
        lock (syncObject)
        {
          if (callbackQueue.Count <= 0)
          {
            engineTread.Abort();
            return;
          }

          e = callbackQueue.Dequeue();         
        }

        e.Dispatch();
      }
    }
  }
}
