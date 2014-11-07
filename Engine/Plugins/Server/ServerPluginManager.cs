using System.Collections.Generic;

namespace Engine.Plugins.Server
{
  public class ServerPluginManager : PluginManager<ServerPlugin>
  {
    private Dictionary<ushort, ServerPluginCommand> commands;

    public ServerPluginManager(string path) : base(path)
    {
      commands = new Dictionary<ushort, ServerPluginCommand>();
    }

    public bool TryGetCommand(ushort id, out IServerCommand command)
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
      foreach (var command in loaded.Plugin.Commands)
        commands.Add(command.Id, command);
    }

    protected override void OnPluginUnlodaing(PluginContainer unloading)
    {
      foreach (var command in unloading.Plugin.Commands)
        commands.Remove(command.Id);
    }
  }
}
