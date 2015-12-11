using Engine.API;
using Engine.Exceptions;
using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.Plugins.Client
{
  [SecurityCritical]
  public class ClientPluginManager : 
    PluginManager<ClientPlugin, ClientModelWrapper, ClientPluginCommand>
  {
    [SecurityCritical]
    public ClientPluginManager(string path)
      : base(path)
    {

    }

    [SecurityCritical]
    internal bool TryGetCommand(long id, out ICommand<ClientCommandArgs> command)
    {
      command = null;

      lock (SyncObject)
      {
        ClientPluginCommand pluginCommand;
        if (Commands.TryGetValue(id, out pluginCommand))
        {
          command = pluginCommand;
          return true;
        }
      }

      return false;
    }

    [SecurityCritical]
    public ClientPlugin GetPlugin(string name)
    {
      lock (SyncObject)
      {
        PluginContainer container;
        if (Plugins.TryGetValue(name, out container))
          return container.Plugin;

        return null;
      }
    }

    [SecurityCritical]
    protected override void OnPluginLoaded(PluginContainer loaded)
    {
      base.OnPluginLoaded(loaded);

      ClientModel.Notifier.PluginLoaded(new PluginEventArgs(loaded.Plugin.Name));
    }

    [SecurityCritical]
    protected override void OnPluginUnlodaing(PluginContainer unloading)
    {
      base.OnPluginUnlodaing(unloading);

      ClientModel.Notifier.PluginUnloading(new PluginEventArgs(unloading.Plugin.Name));
    }

    [SecurityCritical]
    protected override void OnError(string message, Exception e)
    {
      ClientModel.Logger.Write(new ModelException(ErrorCode.PluginError, string.Format("Error: {0}", message), e));
    }
  }
}
