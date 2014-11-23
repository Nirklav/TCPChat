using Engine.Exceptions;
using Engine.Model.Server;
using System;
using System.Collections.Generic;

namespace Engine.Plugins.Server
{
  public class ServerPluginManager : PluginManager<ServerPlugin, ServerModelWrapper>
  {
    private Dictionary<ushort, ServerPluginCommand> commands = new Dictionary<ushort, ServerPluginCommand>();
    private Dictionary<string, ServerNotifierContext> notifierContexts = new Dictionary<string, ServerNotifierContext>();

    public ServerPluginManager(string path)
      : base(path)
    {

    }

    internal bool TryGetCommand(ushort id, out ICommand<ServerCommandArgs> command)
    {
      command = null;

      lock (syncObject)
      {
        ServerPluginCommand pluginCommand;
        if (commands.TryGetValue(id, out pluginCommand))
        {
          command = pluginCommand;
          return true;
        }
      }

      return false;
    }

    internal IEnumerable<ServerNotifierContext> GetNotifierContexts()
    {
      return notifierContexts.Values;
    }

    protected override void OnPluginLoaded(PluginContainer loaded)
    {
      foreach (var command in loaded.Plugin.Commands)
        commands.Add(command.Id, command);

      var context = loaded.Plugin.NotifierContext;
      if (context != null)
        notifierContexts.Add(loaded.Plugin.Name, context);
    }

    protected override void OnPluginUnlodaing(PluginContainer unloading)
    {
      foreach (var command in unloading.Plugin.Commands)
        commands.Remove(command.Id);

      notifierContexts.Remove(unloading.Plugin.Name);
    }

    protected override void OnError(string pluginName, Exception e)
    {
      ServerModel.Logger.Write(new ModelException(ErrorCode.PluginError, string.Format("Error: {0}", pluginName), e));
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
