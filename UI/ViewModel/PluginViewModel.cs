using Engine.Model.Client;
using System;
using UI.Infrastructure;

namespace UI.ViewModel
{
  public class PluginViewModel : BaseViewModel
  {
    public string Header { get; private set; }
    public string PluginName { get; private set; }
    public Command InvokeCommand { get; private set;} 

    public PluginViewModel(string pluginName)
      : base(null, false)
    {
      try
      {
        var plugin = ClientModel.Plugins.GetPlugin(pluginName);
        if (plugin == null)
          return;

        Header = plugin.MenuCaption;
        PluginName = plugin.Name;
        InvokeCommand = new Command(Invoke);
      }
      catch(Exception e)
      {
        ClientModel.Logger.Write(e);
      }
    } 

    public void Invoke(object o)
    {
      try
      {
        var plugin = ClientModel.Plugins.GetPlugin(PluginName);
        if (plugin != null)
          plugin.InvokeMenuHandler();
      }
      catch (Exception e)
      {
        ClientModel.Logger.Write(e);
      }
    }
  }
}
