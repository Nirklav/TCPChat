using Engine.Api;
using Engine.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;

namespace Engine.Network
{
  [SecurityCritical]
  class RequestQueue :
    MarshalByRefObject,
    IDisposable
  {
    private const int Timeout = 1000;

    #region Nested Types

    [SecurityCritical]
    private class QueueContainer : IDisposable
    {
      [SecurityCritical] private readonly RequestQueue _queue;

      [SecurityCritical] private readonly object _syncObject = new object();
      [SecurityCritical] private readonly Queue<CommandArgs> _commands = new Queue<CommandArgs>();
      [SecurityCritical] private volatile bool _inProcess;

      [SecurityCritical] private static readonly TimeSpan _disposeEventTimeout = TimeSpan.FromSeconds(10);
      [SecurityCritical] private readonly ManualResetEvent _disposeEvent = new ManualResetEvent(true);
      [SecurityCritical] private bool _disposed;

      [SecurityCritical]
      public QueueContainer(RequestQueue queue)
      {
        _queue = queue;
      }

      [SecurityCritical]
      public void Enqueue(CommandArgs args)
      {
        using(new TryLock(_syncObject, Timeout))
        {
          _commands.Enqueue(args);
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
          CommandArgs args;

          using (new TryLock(_syncObject, Timeout))
          {
            if (_commands.Count == 0)
            {
              _inProcess = false;
              _disposeEvent.Set();
              return;
            }

            args = _commands.Dequeue();
          }

          try
          {
            using (args)
            {
              var command = _queue._api.GetCommand(args.Unpacked.Package.Id);
              command.Run(args);
            }
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

        _disposeEvent.Close();
      }
    }

    #endregion

    [SecurityCritical] private readonly object _syncObject;
    [SecurityCritical] private readonly Dictionary<string, QueueContainer> _requests;
    [SecurityCritical] private readonly IApi _api;
    [SecurityCritical] private volatile bool _disposed;

    public event EventHandler<AsyncErrorEventArgs> Error;

    [SecurityCritical]
    public RequestQueue(IApi api)
    {
      _api = api;
      _syncObject = new object();
      _requests = new Dictionary<string, QueueContainer>();
    }

    [SecurityCritical]
    internal void Add(string connectionId, Unpacked<IPackage> unpacked)
    {
      ThrowIfDisposed();

      QueueContainer queueContainer;

      using (new TryLock(_syncObject, Timeout))
      {
        _requests.TryGetValue(connectionId, out queueContainer);
        if (queueContainer == null)
          _requests.Add(connectionId, queueContainer = new QueueContainer(this));
      }

      queueContainer.Enqueue(new CommandArgs(connectionId, unpacked));
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

    [SecurityCritical]
    private void OnError(Exception e)
    {
      Error.BeginDispatch(this, new AsyncErrorEventArgs(e), null);
    }
  }
}
