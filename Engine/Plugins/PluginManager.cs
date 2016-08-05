using Engine.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

namespace Engine.Plugins
{
  [SecurityCritical]
  public abstract class PluginManager<TPlugin, TModel, TCommand> : IDisposable
    where TPlugin : Plugin<TModel, TCommand>
    where TModel : CrossDomainObject, new()
    where TCommand : CrossDomainObject, ICommand
  {
    #region nested types

    protected class PluginContainer
    {
      public AppDomain Domain { get; private set; }
      public TPlugin Plugin { get; private set; }
      public TModel Model { get; private set; }
      public bool CommandsLoaded { get; set; }

      public PluginContainer(AppDomain domain, TPlugin plugin, TModel model)
      {
        Domain = domain;
        Plugin = plugin;
        Model = model;
      }
    }

    #endregion

    #region fields

    protected readonly object SyncObject = new object();
    protected readonly Dictionary<string, PluginContainer> Plugins = new Dictionary<string, PluginContainer>();
    protected readonly Dictionary<long, TCommand> Commands = new Dictionary<long, TCommand>();
    protected readonly Dictionary<string, object> NotifierContexts = new Dictionary<string, object>();

    private string path;
    private List<PluginInfo> infos;

    private bool disposed;

    #endregion

    #region Initialization

    [SecurityCritical]
    protected PluginManager(string pluginsPath)
    {
      path = pluginsPath;
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (disposed)
        return;

      disposed = true;

      var containers = Plugins.Values.ToList();
      foreach (var container in containers)
        UnloadPlugin(container);
    }

    #endregion

    #region load

    [SecurityCritical]
    internal void LoadPlugins(string[] excludedPlugins)
    {
      try
      {
        var libs = FindLibraries(path);
        var loader = new PluginInfoLoader();
        infos = loader.LoadFrom(typeof(TPlugin).FullName, libs);
        if (infos != null)
          foreach (var info in infos)
            LoadPlugin(info, excludedPlugins);
      }
      catch (Exception e)
      {
        OnError("Load plugins failed", e);
      }
    }

    [SecurityCritical]
    public void LoadPlugin(string name)
    {
      if (infos == null)
        return;

      var info = infos.Find(pi => pi.Name == name);
      if (info == null)
        return;

      LoadPlugin(info, null);
    }

    [SecurityCritical]
    private void LoadPlugin(PluginInfo info, string[] excludedPlugins)
    {
      lock (SyncObject)
      {
        var domainSetup = new AppDomainSetup();
        domainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
        domainSetup.PrivateBinPath = "plugins;bin";

        var permissions = new PermissionSet(PermissionState.None);
        permissions.AddPermission(new UIPermission(PermissionState.Unrestricted));

        permissions.AddPermission(new SecurityPermission(
          SecurityPermissionFlag.Execution |
          SecurityPermissionFlag.SerializationFormatter |
          SecurityPermissionFlag.UnmanagedCode)); // TODO: wpf need it :(

        permissions.AddPermission(new FileIOPermission(
          FileIOPermissionAccess.PathDiscovery |
          FileIOPermissionAccess.Write |
          FileIOPermissionAccess.Read,
          AppDomain.CurrentDomain.BaseDirectory));

        var engineStrongName = typeof(TPlugin).Assembly.Evidence.GetHostEvidence<StrongName>();
        if (engineStrongName == null)
        {
          OnError("Can't load plugins. Engine library without strong name.", null);
          return;
        }

        var pluginName = string.Empty;
        var domainName = string.Format("Plugin Domain [{0}]", Path.GetFileNameWithoutExtension(info.AssemblyPath));
        var domain = AppDomain.CreateDomain(domainName, null, domainSetup, permissions, engineStrongName);
        try
        {
          var plugin = (TPlugin) domain.CreateInstanceFromAndUnwrap(info.AssemblyPath, info.TypeName);
          info.Name = plugin.Name;
          pluginName = plugin.Name;

          if (Plugins.ContainsKey(pluginName) || (excludedPlugins != null && excludedPlugins.Contains(pluginName)))
          {
            AppDomain.Unload(domain);
            return;
          }

          var model = new TModel();

          plugin.Initialize(model);

          var container = new PluginContainer(domain, plugin, model);
          Plugins.Add(pluginName, container);

          OnPluginLoaded(container);
        }
        catch (Exception e)
        {
          OnError(string.Format("Plugin failed: {0}", pluginName), e);

          if (UnloadPlugin(pluginName))
            return;
                    
          AppDomain.Unload(domain);
        }
      }
    }

    #endregion

    #region unload

    [SecurityCritical]
    public bool UnloadPlugin(string name)
    {
      lock (SyncObject)
      {
        PluginContainer container;
        if (Plugins.TryGetValue(name, out container))
          return UnloadPlugin(container);

        return false;
      }
    }

    [SecurityCritical]
    protected bool UnloadPlugin(PluginContainer container)
    {
      lock (SyncObject)
      {
        OnPluginUnlodaing(container);

        Plugins.Remove(container.Plugin.Name);
        AppDomain.Unload(container.Domain);
        return true;
      }
    }

    #endregion unload

    #region overridable

    [SecurityCritical]
    protected virtual void OnPluginLoaded(PluginContainer loaded)
    {
      // Context
      var context = loaded.Plugin.NotifierContext;
      if (context != null)
        NotifierContexts.Add(loaded.Plugin.Name, context);

      // Commands
      foreach (var command in loaded.Plugin.Commands)
        if (Commands.ContainsKey(command.Id))
          throw new ArgumentException(string.Format("In manager already loaded plugin with same command id [CommandId: {0}]", command.Id));

      foreach (var command in loaded.Plugin.Commands)
        Commands.Add(command.Id, command);

      loaded.CommandsLoaded = true;
    }

    [SecurityCritical]
    protected virtual void OnPluginUnlodaing(PluginContainer unloading)
    {
      // Context
      NotifierContexts.Remove(unloading.Plugin.Name);

      // Commands
      if (unloading.CommandsLoaded)
      {
        foreach (var command in unloading.Plugin.Commands)
          Commands.Remove(command.Id);
      }
    }

    [SecurityCritical]
    protected virtual void OnError(string message, Exception e) { }

    #endregion

    #region helpers

    [SecurityCritical]
    internal bool TryGetCommand(long id, out TCommand command)
    {
      lock (SyncObject)
        return Commands.TryGetValue(id, out command);
    }

    [SecurityCritical]
    internal IEnumerable GetNotifierContexts()
    {
      lock (SyncObject)
        return NotifierContexts.Values.ToArray();
    }

    [SecurityCritical]
    public bool IsLoaded(string name)
    {
      lock (SyncObject)
        return Plugins.ContainsKey(name);
    }

    [SecurityCritical]
    public string[] GetPlugins()
    {
      lock (SyncObject)
      {
        if (infos == null)
          return null;
        return infos.Select(pi => pi.Name).ToArray();
      }
    }

    [SecurityCritical]
    private static string[] FindLibraries(string path)
    {
      if (!Directory.Exists(path))
        return new string[0];

      return Directory
        .GetFiles(path)
        .Where(f => string.Equals(Path.GetExtension(f), ".dll", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    }

    #endregion
  }
}
