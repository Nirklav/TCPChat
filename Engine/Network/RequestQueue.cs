using Engine.Api;
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
      ClientModel.Notifier.AsyncError(new AsyncErrorEventArgs(exc));
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
      [SecurityCritical] private readonly RequestQueue<TArgs> _queue;

      [SecurityCritical] private readonly object _syncObject = new object();
      [SecurityCritical] private readonly Queue<CommandContainer> _commands = new Queue<CommandContainer>();
      [SecurityCritical] private volatile bool _inProcess;

      [SecurityCritical] private static readonly TimeSpan _disposeEventTimeout = TimeSpan.FromSeconds(10);
      [SecurityCritical] private readonly ManualResetEvent _disposeEvent = new ManualResetEvent(true);
      [SecurityCritical] private bool _disposed;

      [SecurityCritical]
      public QueueContainer(RequestQueue<TArgs> queue)
      {
        _queue = queue;
      }

      [SecurityCritical]
      public void Enqueue(ICommand<TArgs> command, TArgs args)
      {
        using(new TryLock(_syncObject, Timeout))
        {
          _commands.Enqueue(new CommandContainer(command, args));
          if (!_inProcess)
          {
            _inProcess = true;
            _disposeEvent.Reset();
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

          using (new TryLock(_syncObject, Timeout))
          {
            if (_commands.Count == 0)
            {
              _inProcess = false;
              _disposeEvent.Set();
              return;
            }

            commandContainer = _commands.Dequeue();
          }

          try
          {
            commandContainer.Run();
          }
          catch (Exception e)
          {
            _queue.OnError(e);
          }
        }
      }

      [SecuritySafeCritical]
      public void Dispose()
      {
        if (_disposed)
          return;

        _disposed = true;

        lock (_syncObject)
          _commands.Clear();

        if (!_disposeEvent.WaitOne(_disposeEventTimeout))
          _queue.OnError(new Exception("RequestQueue.Dispose() timeout"));
      }
    }

    [SecurityCritical]
    private class CommandContainer
    {
      [SecurityCritical] private ICommand<TArgs> _command;
      [SecurityCritical] private TArgs _args;

      [SecurityCritical]
      public CommandContainer(ICommand<TArgs> command, TArgs args)
      {
        _command = command;
        _args = args;
      }

      [SecurityCritical]
      public void Run()
      {
        using (_args)
          _command.Run(_args);
      }
    }

    #endregion

    [SecurityCritical] private readonly object _syncObject;
    [SecurityCritical] private readonly Dictionary<string, QueueContainer> _requests;

    [SecurityCritical] private volatile bool _disposed;

    [SecurityCritical]
    public RequestQueue()
    {
      _syncObject = new object();
      _requests = new Dictionary<string, QueueContainer>();
    }

    [SecurityCritical]
    internal void Add(string connectionId, ICommand<TArgs> command, TArgs args)
    {
      ThrowIfDisposed();

      QueueContainer queueContainer;

      using (new TryLock(_syncObject, Timeout))
      {
        _requests.TryGetValue(connectionId, out queueContainer);
        if (queueContainer == null)
          _requests.Add(connectionId, queueContainer = new QueueContainer(this));
      }

      queueContainer.Enqueue(command, args);
    }

    internal void Clean()
    {
      QueueContainer[] queues;

      lock (_syncObject)
      {
        queues = _requests.Values.ToArray();
        _requests.Clear();
      }

      foreach (var queue in queues)
        queue.Dispose();
    }

    [SecurityCritical]
    protected abstract void OnError(Exception e);

    [SecurityCritical]
    private void ThrowIfDisposed()
    {
      if (_disposed)
        throw new ObjectDisposedException("RequestQueue is disposed");
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;
      Clean();
    }
  }
}
