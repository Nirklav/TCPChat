using Engine.Model.Client;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Threading;

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

  abstract class RequestQueue<TArgs> : MarshalByRefObject
  {
    private const int Timeout = 1000;

    #region Nested Types

    private class QueueContainer
    {
      private RequestQueue<TArgs> queue;

      private volatile bool inProcess;
      private object syncObject = new object();
      private Queue<CommandContainer> commands = new Queue<CommandContainer>();

      public QueueContainer(RequestQueue<TArgs> queue)
      {
        this.queue = queue;
      }

      public void Enqueue(ICommand<TArgs> command, TArgs args)
      {
        var sync = syncObject;
        var lockTaken = false;

        try
        {
          lockTaken = Monitor.TryEnter(sync, Timeout);
          if (!lockTaken)
            throw new InvalidOperationException("Monitor.TryEnter timeout");

          commands.Enqueue(new CommandContainer(command, args));
          if (!inProcess)
          {
            inProcess = true;
            ThreadPool.QueueUserWorkItem(Process);
          }
        }
        finally
        {
          if (lockTaken)
            Monitor.Exit(sync);
        }
      }

      private void Process(object o)
      {
        while (true)
        {
          CommandContainer commandContainer;
          var sync = syncObject;
          var lockTaken = false;

          try
          {
            lockTaken = Monitor.TryEnter(sync, Timeout);
            if (!lockTaken)
              throw new InvalidOperationException("Monitor.TryEnter timeout");

            if (commands.Count == 0)
            {
              inProcess = false;
              return;
            }

            commandContainer = commands.Dequeue();
          }
          finally
          {
            if (lockTaken)
              Monitor.Exit(sync);
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

    private object syncObject;
    private Dictionary<string, QueueContainer> requests;

    public RequestQueue()
    {
      syncObject = new object();
      requests = new Dictionary<string, QueueContainer>();
    }

    internal void Add(string connectionId, ICommand<TArgs> command, TArgs args)
    {
      QueueContainer queueContainer;

      var sync = syncObject;
      var lockTaken = false;

      try
      {
        lockTaken = Monitor.TryEnter(sync, Timeout);
        if (!lockTaken)
          throw new InvalidOperationException("Monitor.TryEnter timeout");

        requests.TryGetValue(connectionId, out queueContainer);
        if (queueContainer == null)
          requests.Add(connectionId, (queueContainer = new QueueContainer(this)));
      }
      finally
      {
        if (lockTaken)
          Monitor.Exit(sync);
      }

      queueContainer.Enqueue(command, args);
    }

    protected abstract void OnError(Exception e);
  }
}
