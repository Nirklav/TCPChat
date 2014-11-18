using Engine.Model.Client;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
      ClientModel.OnAsyncError(this, new AsyncErrorEventArgs { Error = exc });
      ClientModel.Logger.Write(exc);
    }
  }

  abstract class RequestQueue<TArgs>
  {
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
        lock (syncObject)
        {
          commands.Enqueue(new CommandContainer(command, args));
          if (!inProcess)
          {
            inProcess = true;
            ThreadPool.QueueUserWorkItem(Process);
          }
        }
      }

      private void Process(object o)
      {
        while (true)
        {
          CommandContainer commandContainer;
          lock (syncObject)
          {
            if (commands.Count == 0)
            {
              inProcess = false;
              return;
            }

            commandContainer = commands.Dequeue();
          }

          try
          {
            commandContainer.Run();
          }
          catch(Exception e)
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

    public void Add(string connectionId, ICommand<TArgs> command, TArgs args)
    {
      QueueContainer queueContainer;

      lock (syncObject)
      {
        requests.TryGetValue(connectionId, out queueContainer);
        if (queueContainer == null)
          requests.Add(connectionId, (queueContainer = new QueueContainer(this)));
      }

      queueContainer.Enqueue(command, args);
    }

    protected abstract void OnError(Exception e);
  }
}
