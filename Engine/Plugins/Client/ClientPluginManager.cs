using Engine.Exceptions;
using Engine.Model.Client;
using System;
using System.Collections.Generic;

namespace Engine.Plugins.Client
{
  public class ClientPluginManager : PluginManager<ClientPlugin, ClientModelWrapper>
  {
    private Dictionary<ushort, ClientPluginCommand> commands = new Dictionary<ushort, ClientPluginCommand>();

    public ClientPluginManager(string path)
      : base(path)
    {

    }

    internal bool TryGetCommand(ushort id, out ICommand<ClientCommandArgs> command)
    {
      command = null;

      lock (syncObject)
      {
        ClientPluginCommand pluginCommand;
        if (commands.TryGetValue(id, out pluginCommand))
        {
          command = pluginCommand;
          return true;
        }
      }

      return false;
    }

    public ClientPlugin GetPlugin(string name)
    {
      lock (syncObject)
      {
        PluginContainer container;
        if (plugins.TryGetValue(name, out container))
          return container.Plugin;

        return null;
      }
    }

    protected override void OnPluginLoaded(PluginContainer loaded)
    {
      base.OnPluginLoaded(loaded);

      foreach (var command in loaded.Plugin.Commands)
        commands.Add(command.Id, command);

      ClientModel.Notifier.PluginLoaded(new PluginEventArgs(loaded.Plugin.Name));
    }

    protected override void OnPluginUnlodaing(PluginContainer unloading)
    {
      base.OnPluginUnlodaing(unloading);

      foreach (var command in unloading.Plugin.Commands)
        commands.Remove(command.Id);

      ClientModel.Notifier.PluginUnloading(new PluginEventArgs(unloading.Plugin.Name));
    }

    protected override void OnError(string pluginName, Exception e)
    {
      ClientModel.Logger.Write(new ModelException(ErrorCode.PluginError, string.Format("Error: {0}", pluginName), e));
    }

    protected override void Process()
    {
      lock (syncObject)
      {
        foreach (var command in commands.Values)
          command.Process();
      }
    }
  }
}
