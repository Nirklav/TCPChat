using Engine.Model.Client;
using Engine.Model.Server;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using UI.Infrastructure;

namespace UI.ViewModel
{
  public class PluginSettingTabViewModel : SettingsTabViewModel
  {
    private const string NameKey = "settingsTabCategory-plugins";

    public PluginInfoViewModel SelectedPlugin { get; set; }
    public ObservableCollection<PluginInfoViewModel> Plugins { get; private set; }

    public Command LoadCommand { get; private set; }
    public Command UnloadCommand { get; private set; }

    public PluginSettingTabViewModel()
      : base(NameKey, SettingsTabCategory.Plugins) 
    {
      Plugins = new ObservableCollection<PluginInfoViewModel>();

      LoadCommand = new Command(Load, o => ClientModel.IsInited || ServerModel.IsInited);
      UnloadCommand = new Command(Unload, o => ClientModel.IsInited || ServerModel.IsInited);

      Refresh();
    }

    public void Refresh()
    {
      Plugins.Clear();

      if (ClientModel.IsInited)
        AddPlugins(ClientModel.Plugins.IsLoaded, ClientModel.Plugins.GetPlugins(), PluginKindId.Client);

      if (ServerModel.IsInited)
        AddPlugins(ServerModel.Plugins.IsLoaded, ServerModel.Plugins.GetPlugins(), PluginKindId.Server);
    }

    private void AddPlugins(Func<string, bool> isLoadedFunc, string[] plugins, PluginKindId kind)
    {
      if (plugins == null)
        return;

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
      if (SelectedPlugin == null)
        return;

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
      if (SelectedPlugin == null)
        return;

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
        ? ClientModel.IsInited && ClientModel.Plugins.IsLoaded(plugin.Name)
        : ServerModel.IsInited && ServerModel.Plugins.IsLoaded(plugin.Name);
    }
  }
}
