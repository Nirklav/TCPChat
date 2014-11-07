using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Engine.Plugins
{
  public abstract class PluginManager<TPlugin, TModel> : IDisposable
    where TPlugin : Plugin<TModel>
    where TModel : IPluginModelWrapper, new()
  {
    #region Nested types

    protected class PluginContainer
    {
      public AppDomain Domain { get; private set; }
      public TPlugin Plugin { get; private set; }

      public PluginContainer(AppDomain domain, TPlugin plugin)
      {
        Domain = domain;
        Plugin = plugin;
      }
    }

    #endregion

    protected object syncObject = new object();
    protected Dictionary<string, PluginContainer> plugins;
    protected TModel model;

    private Thread processThread;

    protected PluginManager(string path)
    {
      plugins = new Dictionary<string, PluginContainer>();
      model = new TModel();

      var infoLoader = new PluginInfoLoader();
      var libs = FindLibraries(path);

      var infos = infoLoader.LoadFrom(typeof(TPlugin).FullName, libs);
      foreach (var info in infos)
        LoadPlugin(info);

      processThread = new Thread(ProcessThreadHandler);
      processThread.IsBackground = true;
      processThread.Start();
    }

    public void Dispose()
    {
      var containers = plugins.Values.ToList();
      foreach (var container in containers)
        UnloadPlugin(container);
    }

    private void LoadPlugin(PluginInfo info)
    {
      lock (syncObject)
      {
        var domain = AppDomain.CreateDomain(string.Format("Plugin Domain [{0}]", Path.GetFileNameWithoutExtension(info.AssemblyPath)));
        var pluginName = string.Empty;
        try
        {
          var plugin = (TPlugin)domain.CreateInstanceFromAndUnwrap(info.AssemblyPath, info.TypeName);
          pluginName = plugin.Name;

          if (plugins.ContainsKey(pluginName))
          {
            AppDomain.Unload(domain);
            return;
          }

          plugin.Initialize(model);

          var container = new PluginContainer(domain, plugin);
          plugins.Add(pluginName, container);

          OnPluginLoaded(container);
        }
        catch (Exception e)
        {
          OnError(pluginName, e);
          AppDomain.Unload(domain);
          return;
        }
      }
    }

    protected virtual void OnPluginLoaded(PluginContainer loaded) { }
    protected virtual void OnPluginUnlodaing(PluginContainer unloading) { }
    protected virtual void OnError(string pluginName, Exception e) { }

    protected void UnloadPlugin(string name)
    {
      lock (syncObject)
      {
        PluginContainer container;
        if (plugins.TryGetValue(name, out container))
          UnloadPlugin(container);
      }
    }

    protected void UnloadPlugin(PluginContainer container)
    {
      lock (syncObject)
      {
        OnPluginUnlodaing(container);

        plugins.Remove(container.Plugin.Name);
        AppDomain.Unload(container.Domain);
      }
    }

    protected void ProcessThreadHandler()
    {
      while (true)
      {
        Thread.Sleep(TimeSpan.FromMinutes(1));

        lock (syncObject)
        {
          // update cross domain wrappers

          foreach (var container in plugins.Values)
            container.Plugin.Process();
        }
      }
    }

    private static string[] FindLibraries(string path)
    {
      return Directory.GetFiles(path).Where(f => f.Contains(".dll")).ToArray();
    }
  }
}
