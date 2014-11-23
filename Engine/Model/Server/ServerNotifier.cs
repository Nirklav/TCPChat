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
  }

  public abstract class ServerNotifierContext : CrossDomainObject
  {
    protected internal virtual void OnRegistered(ServerRegistrationEventArgs args) { }
    protected internal virtual void OnUnregistered(ServerRegistrationEventArgs args) { }
  }
}
