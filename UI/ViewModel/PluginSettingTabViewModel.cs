using Engine.Model.Client;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UI.Infrastructure;

namespace UI.ViewModel
{
  public class PluginSettingTabViewModel : SettingsTabViewModel
  {
    public PluginInfoViewModel SelectedPlugin { get; set; }
    public ObservableCollection<PluginInfoViewModel> Plugins { get; private set; }

    public Command LoadCommand { get; private set; }
    public Command UnloadCommand { get; private set; }

    public PluginSettingTabViewModel(string name) : base(name) 
    {
      Plugins = new ObservableCollection<PluginInfoViewModel>();

      LoadCommand = new Command(Load);
      UnloadCommand = new Command(Unload);

      Refresh();
    }

    public override void Dispose()
    {
      base.Dispose();
    }

    public void Refresh()
    {
      Plugins.Clear();

      AddPlugins(ClientModel.Plugins.IsLoaded, ClientModel.Plugins.GetPlugins(), PluginKindId.Client);
      AddPlugins(ServerModel.Plugins.IsLoaded, ServerModel.Plugins.GetPlugins(), PluginKindId.Server);
    }

    private void AddPlugins(Func<string, bool> isLoadedFunc, string[] plugins, PluginKindId kind)
    {
      foreach (var pluginName in plugins)
      {
        var plugin = new PluginSetting(pluginName, isLoadedFunc(pluginName));
        Plugins.Add(new PluginInfoViewModel(plugin, kind));

        var saved = Settings.Current.Plugins.Find(p => p.Name == pluginName);
        if (saved != null)
          plugin.Enabled = saved.Enabled;
      }
    }

    public override void SaveSettings()
    {
      Settings.Current.Plugins = Plugins.Select(pi => pi.ToSetting()).ToList();
    }

    private void Load(object obj)
    {
      if (IsLoaded(SelectedPlugin))
        return;

      switch (SelectedPlugin.Kind)
      {
        case PluginKindId.Server:
          ServerModel.Plugins.LoadPlugin(SelectedPlugin.Name);
          break;
        case PluginKindId.Client:
          ClientModel.Plugins.LoadPlugin(SelectedPlugin.Name);
          break;
      }

      Refresh();
    }

    private void Unload(object obj)
    {
      if (!IsLoaded(SelectedPlugin))
        return;

      switch (SelectedPlugin.Kind)
      {
        case PluginKindId.Server:
          ServerModel.Plugins.UnloadPlugin(SelectedPlugin.Name);
          break;
        case PluginKindId.Client:
          ClientModel.Plugins.UnloadPlugin(SelectedPlugin.Name);
          break;
      }

      Refresh();
    }

    private static bool IsLoaded(PluginInfoViewModel plugin)
    {
      return plugin.Kind == PluginKindId.Client
        ? ClientModel.Plugins.IsLoaded(plugin.Name)
        : ServerModel.Plugins.IsLoaded(plugin.Name);
    }
  }
}
