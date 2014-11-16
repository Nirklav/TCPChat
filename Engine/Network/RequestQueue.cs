using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Engine.Network
{
  class RequestQueue<TCommand, TArgs>
    where TCommand : ICommand<TArgs>
  {
    #region Nested Types

    private class QueueContainer
    {
      private volatile bool inProcess;
      private object syncObject = new object();
      private Queue<CommandContainer> commands = new Queue<CommandContainer>();

      public void Enqueue(TCommand command, TArgs args)
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

          var command = commandContainer.Command;
          var args = commandContainer.Args;

          command.Run(args);
        }
      }
    }

    private class CommandContainer
    {
      public CommandContainer(TCommand command, TArgs args)
	    {
        Command = command;
        Args = args;
	    }

      public TCommand Command { get; private set; }
      public TArgs Args { get; private set; }
    }

    #endregion

    private object syncObject;
    private Dictionary<string, QueueContainer> requests;

    public RequestQueue()
    {
      syncObject = new object();
      requests = new Dictionary<string, QueueContainer>();
    }

    public void Add(string connectionId, TCommand command, TArgs args)
    {
      QueueContainer queueContainer;

      lock (syncObject)
      {
        requests.TryGetValue(connectionId, out queueContainer);
        if (queueContainer == null)
          requests.Add(connectionId, (queueContainer = new QueueContainer()));
      }

      queueContainer.Enqueue(command, args);
    }
  }
}
