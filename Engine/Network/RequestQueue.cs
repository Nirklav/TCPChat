using Engine.API;
using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;

namespace Engine.Network
{
  [SecurityCritical]
  class ServerRequestQueue : RequestQueue<ServerCommandArgs>
  {
    [SecurityCritical]
    protected override void OnError(Exception exc)
    {
      ServerModel.Logger.Write(exc);
    }
  }

  [SecurityCritical]
  class ClientRequestQueue : RequestQueue<ClientCommandArgs>
  {
    [SecurityCritical]
    protected override void OnError(Exception exc)
    {
      ClientModel.Notifier.AsyncError(new AsyncErrorEventArgs { Error = exc });
      ClientModel.Logger.Write(exc);
    }
  }

  [SecurityCritical]
  abstract class RequestQueue<TArgs> :
    MarshalByRefObject,
    IDisposable
    where TArgs : CommandArgs
  {
    private const int Timeout = 1000;

    #region Nested Types

    [SecurityCritical]
    private class QueueContainer : IDisposable
    {
      [SecurityCritical] private readonly RequestQueue<TArgs> queue;

      [SecurityCritical] private readonly object syncObject = new object();
      [SecurityCritical] private readonly Queue<CommandContainer> commands = new Queue<CommandContainer>();
      [SecurityCritical] private volatile bool inProcess;

      [SecurityCritical] private static readonly TimeSpan disposeEventTimeout = TimeSpan.FromSeconds(10);
      [SecurityCritical] private readonly ManualResetEvent disposeEvent = new ManualResetEvent(true);
      [SecurityCritical] private bool disposed;

      [SecurityCritical]
      public QueueContainer(RequestQueue<TArgs> queue)
      {
        this.queue = queue;
      }

      [SecurityCritical]
      public void Enqueue(ICommand<TArgs> command, TArgs args)
      {
        using(new TryLock(syncObject, Timeout))
        {
          commands.Enqueue(new CommandContainer(command, args));
          if (!inProcess)
          {
            inProcess = true;
            disposeEvent.Reset();
            ThreadPool.QueueUserWorkItem(Process);
          }
        }
      }

      [SecurityCritical]
      private void Process(object o)
      {
        while (true)
        {
          CommandContainer commandContainer;

          using (new TryLock(syncObject, Timeout))
          {
            if (commands.Count == 0)
            {
              inProcess = false;
              disposeEvent.Set();
              return;
            }

            commandContainer = commands.Dequeue();
          }

          try
          {
            commandContainer.Run();
          }
          catch (Exception e)
          {
            queue.OnError(e);
          }
        }
      }

      [SecuritySafeCritical]
      public void Dispose()
      {
        if (disposed)
          return;

        disposed = true;

        lock (syncObject)
          commands.Clear();

        if (!disposeEvent.WaitOne(disposeEventTimeout))
          queue.OnError(new Exception("RequestQueue.Dispose() timeout"));
      }
    }

    [SecurityCritical]
    private class CommandContainer
    {
      [SecurityCritical] private ICommand<TArgs> command;
      [SecurityCritical] private TArgs args;

      [SecurityCritical]
      public CommandContainer(ICommand<TArgs> command, TArgs args)
      {
        this.command = command;
        this.args = args;
      }

      [SecurityCritical]
      public void Run()
      {
        command.Run(args);
      }
    }

    #endregion

    [SecurityCritical] private readonly object syncObject;
    [SecurityCritical] private readonly Dictionary<string, QueueContainer> requests;

    [SecurityCritical] private volatile bool disposed;

    [SecurityCritical]
    public RequestQueue()
    {
      syncObject = new object();
      requests = new Dictionary<string, QueueContainer>();
    }

    [SecurityCritical]
    internal void Add(string connectionId, ICommand<TArgs> command, TArgs args)
    {
      ThrowIfDisposed();

      QueueContainer queueContainer;

      using (new TryLock(syncObject, Timeout))
      {
        requests.TryGetValue(connectionId, out queueContainer);
        if (queueContainer == null)
          requests.Add(connectionId, queueContainer = new QueueContainer(this));
      }

      queueContainer.Enqueue(command, args);
    }

    [SecurityCritical]
    protected abstract void OnError(Exception e);

    [SecurityCritical]
    private void ThrowIfDisposed()
    {
      if (disposed)
        throw new ObjectDisposedException("RequestQueue is disposed");
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (disposed)
        return;

      disposed = true;

      QueueContainer[] queues;

      lock (syncObject)
      {
        queues = requests.Values.ToArray();
        requests.Clear();
      }

      foreach (var queue in queues)
        queue.Dispose();
    }
  }
}
