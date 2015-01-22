using Engine.Model.Common;
using System;
using System.Collections.Generic;

namespace Engine.Model.Server
{
  public class ServerNotifier : Notifier
  {
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
