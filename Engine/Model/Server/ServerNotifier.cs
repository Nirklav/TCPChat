using Engine.Model.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Model.Server
{
  [SecuritySafeCritical]
  public class ServerNotifier : Notifier
  {
    [SecuritySafeCritical]
    public override IEnumerable<object> GetEvents()
    {
      var events = base.GetEvents();
      return events.Concat(ServerModel.Plugins.GetNotifierEvents());
    }
  }

  [Notifier(typeof(IServerEvents), BaseNotifier = typeof(ServerNotifier))]
  public interface IServerNotifier : INotifier
  {
    void StartError(AsyncErrorEventArgs args);

    void ConnectionOpened(ConnectionEventArgs args);
    void ConnectionClosing(ConnectionEventArgs args, Action<Exception> callback);
    void ConnectionClosed(ConnectionEventArgs args);

    void ConnectionRegistered(ConnectionEventArgs args);
    void ConnectionUnregistered(ConnectionEventArgs args);
  }

  public interface IServerEvents
  {
    event EventHandler<AsyncErrorEventArgs> StartError;

    event EventHandler<ConnectionEventArgs> ConnectionOpened;
    event EventHandler<ConnectionEventArgs> ConnectionClosing;
    event EventHandler<ConnectionEventArgs> ConnectionClosed;

    event EventHandler<ConnectionEventArgs> ConnectionRegistered;
    event EventHandler<ConnectionEventArgs> ConnectionUnregistered;
  }
}
