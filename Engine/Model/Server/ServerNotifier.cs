using Engine.Model.Common;
using System;
using System.Collections.Generic;
using System.Security;

namespace Engine.Model.Server
{
  [SecuritySafeCritical]
  public class ServerNotifier : Notifier
  {
    [SecuritySafeCritical]
    public override object[] GetContexts()
    {
      var contexts = new List<object>(base.GetContexts());

      foreach (var context in ServerModel.Plugins.GetNotifierContexts())
        contexts.Add(context);

      return contexts.ToArray();
    }
  }

  [Notifier(typeof(IServerNotifierContext), BaseNotifier = typeof(ServerNotifier))]
  public interface IServerNotifier
  {
    void Registered(ServerRegistrationEventArgs args);
    void Unregistered(ServerRegistrationEventArgs args);
  }

  public interface IServerNotifierContext
  {
    event EventHandler<ServerRegistrationEventArgs> Registered;
    event EventHandler<ServerRegistrationEventArgs> Unregistered;
  }
}
