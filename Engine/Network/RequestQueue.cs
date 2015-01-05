using Engine.Model.Client;
using Engine.Model.Server;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Engine.Helpers;

namespace Engine.Network
{
  class ServerRequestQueue : RequestQueue<ServerCommandArgs>
  {
    protected override void OnError(Exception exc)
    {
      ServerModel.Logger.Write(exc);
    }
  }

  class ClientRequestQueue : RequestQueue<ClientCommandArgs>
  {
    protected override void OnError(Exception exc)
    {
      ClientModel.Notifier.AsyncError(new AsyncErrorEventArgs { Error = exc });
      ClientModel.Logger.Write(exc);
    }
  }

  abstract class RequestQueue<TArgs> :
    MarshalByRefObject,
    IDisposable
  {
    private const int Timeout = 1000;

    #region Nested Types

    private class QueueContainer : IDisposable
    {
      private readonly RequestQueue<TArgs> queue;

      private readonly object syncObject = new object();
      private readonly Queue<CommandContainer> commands = new Queue<CommandContainer>();
      private volatile bool inProcess;

      private static readonly TimeSpan disposeEventTimeout = TimeSpan.FromSeconds(10);
      private readonly ManualResetEvent disposeEvent = new ManualResetEvent(true);
      private bool disposed;

      public QueueContainer(RequestQueue<TArgs> queue)
      {
        this.queue = queue;
      }

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

      public void Dispose()
      {
        if (disposed)
          return;

        disposed = true;

        lock (syncObject)
          commands.Clear();

        disposeEvent.WaitOne(disposeEventTimeout);
      }
    }

    private class CommandContainer
    {
      private ICommand<TArgs> command;
      private TArgs args;

      public CommandContainer(ICommand<TArgs> command, TArgs args)
      {
        this.command = command;
        this.args = args;
      }

      public void Run()
      {
        command.Run(args);
      }
    }

    #endregion

    private readonly object syncObject;
    private readonly Dictionary<string, QueueContainer> requests;

    private bool disposed;

    public RequestQueue()
    {
      syncObject = new object();
      requests = new Dictionary<string, QueueContainer>();
    }

    internal void Add(string connectionId, ICommand<TArgs> command, TArgs args)
    {
      QueueContainer queueContainer;

      using (new TryLock(syncObject, Timeout))
      {
        requests.TryGetValue(connectionId, out queueContainer);
        if (queueContainer == null)
          requests.Add(connectionId, queueContainer = new QueueContainer(this));
      }

      queueContainer.Enqueue(command, args);
    }

    protected abstract void OnError(Exception e);

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
