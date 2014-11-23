using Engine.Exceptions;
using Engine.Model.Client;
using System;
using System.Collections.Generic;

namespace Engine.Plugins.Client
{
  public class ClientPluginManager : PluginManager<ClientPlugin, ClientModelWrapper>
  {
    private Dictionary<ushort, ClientPluginCommand> commands = new Dictionary<ushort, ClientPluginCommand>();
    private Dictionary<string, ClientNotifierContext> notifierContexts = new Dictionary<string, ClientNotifierContext>();

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

    internal IEnumerable<ClientNotifierContext> GetNotifierContexts()
    {
      return notifierContexts.Values;
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
      foreach (var command in loaded.Plugin.Commands)
        commands.Add(command.Id, command);

      ClientModel.Notifier.PluginLoaded(new PluginEventArgs(loaded.Plugin));

      var context = loaded.Plugin.NotifierContext;
      if (context != null)
        notifierContexts.Add(loaded.Plugin.Name, context);
    }

    protected override void OnPluginUnlodaing(PluginContainer unloading)
    {
      foreach (var command in unloading.Plugin.Commands)
        commands.Remove(command.Id);

      ClientModel.Notifier.PluginUnloading(new PluginEventArgs(unloading.Plugin));

      notifierContexts.Remove(unloading.Plugin.Name);
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

        foreach (var context in notifierContexts.Values)
          context.Process();
      }
    }
  }
}
