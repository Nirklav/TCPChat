using Engine.Exceptions;
using Engine.Model.Server;
using System;
using System.Collections.Generic;

namespace Engine.Plugins.Server
{
  public class ServerPluginManager : PluginManager<ServerPlugin, ServerModelWrapper>
  {
    private Dictionary<ushort, ServerPluginCommand> commands = new Dictionary<ushort, ServerPluginCommand>();

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

    protected override void OnPluginLoaded(PluginContainer loaded)
    {
      base.OnPluginLoaded(loaded);

      foreach (var command in loaded.Plugin.Commands)
        commands.Add(command.Id, command);
    }

    protected override void OnPluginUnlodaing(PluginContainer unloading)
    {
      base.OnPluginUnlodaing(unloading);

      foreach (var command in unloading.Plugin.Commands)
        commands.Remove(command.Id);
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
      }
    }
  }
}
