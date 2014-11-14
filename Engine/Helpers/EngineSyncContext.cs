using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Engine.Helpers
{
  class EngineSyncContext : SynchronizationContext
  {
    private class Event : IDisposable
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

      public void Dispose()
      {
        resetEvent.Close();
      }
    }

    private Queue<Event> callbackQueue;
    private Thread engineTread;

    public EngineSyncContext()
	  {
      callbackQueue = new Queue<Event>();
      engineTread = new Thread(ThreadFunc);
      engineTread.IsBackground = true;
      engineTread.Start();
	  }

    public override void Post(SendOrPostCallback d, object state)
    {
      lock (callbackQueue)
      {
        callbackQueue.Enqueue(new Event(d, state, false));
      }
    }

    public override void Send(SendOrPostCallback d, object state)
    {
      Event item = null;
      lock (callbackQueue)
      {
        item = new Event(d, state, true);
        callbackQueue.Enqueue(item);
      }

      if (item != null)
        item.Handle.WaitOne();
    }

    public override SynchronizationContext CreateCopy()
    {
      return this;
    }

    private void ThreadFunc()
    {
      while(true)
      {
        lock(callbackQueue)
        {
          if (callbackQueue.Count <= 0)
          {
            Monitor.PulseAll(callbackQueue);
            continue;
          }

          var e = callbackQueue.Dequeue();
          e.Dispatch();
        }
      }
    }
  }
}
