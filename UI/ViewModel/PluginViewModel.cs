using Engine.Model.Client;
using Engine.Plugins.Client;
using System;
using UI.Infrastructure;

namespace UI.ViewModel
{
  public class PluginViewModel : BaseViewModel
  {
    public string Header { get; private set; }
    public string PluginName { get; private set; }
    public Command InvokeCommand { get; private set;} 

    public PluginViewModel(ClientPlugin plugin)
      : base(false)
    {
      try
      {
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
