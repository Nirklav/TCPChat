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
    public ObservableCollection<PluginSetting> Plugins { get; private set; }

    public PluginSettingTabViewModel(string name) : base(name) 
    {
      Plugins = new ObservableCollection<PluginSetting>();

      AddPlugins(ClientModel.Plugins.GetPlugins());
      AddPlugins(ServerModel.Plugins.GetPlugins());
    }

    private void AddPlugins(List<string> plugins)
    {
      foreach (var pluginName in plugins)
        Plugins.Add(new PluginSetting(pluginName));
    }

    public override void Dispose()
    {
      base.Dispose();
    }

    public override void SaveSettings()
    {
      Settings.Current.Plugins = Plugins.ToList();
    }
  }
}
