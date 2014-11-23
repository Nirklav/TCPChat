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
    public ObservableCollection<PluginInfoViewModel> Plugins { get; private set; }

    public PluginSettingTabViewModel(string name) : base(name) 
    {
      Plugins = new ObservableCollection<PluginInfoViewModel>();

      AddPlugins(ClientModel.Plugins.GetPlugins(), PluginKindId.Client);
      AddPlugins(ServerModel.Plugins.GetPlugins(), PluginKindId.Server);
    }

    private void AddPlugins(List<string> plugins, PluginKindId kind)
    {
      foreach (var pluginName in plugins)
      {
        var plugin = new PluginSetting(pluginName);
        Plugins.Add(new PluginInfoViewModel(plugin, kind));

        var saved = Settings.Current.Plugins.Find(p => p.Name == pluginName);
        if (saved != null)
          plugin.Enabled = saved.Enabled;
      }
    }

    public override void Dispose()
    {
      base.Dispose();
    }

    public override void SaveSettings()
    {
      Settings.Current.Plugins = Plugins.Select(pi => pi.ToSetting()).ToList();
    }
  }
}
