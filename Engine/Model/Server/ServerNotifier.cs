using Engine.Model.Common;
using Engine.Plugins;
using System;

namespace Engine.Model.Server
{
  public class ServerNotifier : Notifier<ServerNotifierContext>
  {
    protected internal virtual void OnRegistered(ServerRegistrationEventArgs args)
    {
      Notify((c, a) => c.OnRegistered(a), args);
    }

    protected internal virtual void OnUnregistered(ServerRegistrationEventArgs args)
    {
      Notify((c, a) => c.OnUnregistered(a), args);
    }

    protected override void Notify<TArgs>(Action<ServerNotifierContext, TArgs> methodInvoker, TArgs args)
    {
      base.Notify<TArgs>(methodInvoker, args);

      foreach (var context in ServerModel.Plugins.GetNotifierContexts())
        methodInvoker(context, args);
    }
  }

  public abstract class ServerNotifierContext : CrossDomainObject
  {
    protected internal virtual void OnRegistered(ServerRegistrationEventArgs args) { }
    protected internal virtual void OnUnregistered(ServerRegistrationEventArgs args) { }
  }
}
