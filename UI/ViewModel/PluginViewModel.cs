using Engine.Model.Client;
using Engine.Plugins.Client;
using UI.Infrastructure;

namespace UI.ViewModel
{
  public class PluginViewModel : BaseViewModel
  {
    public string Header { get; private set; }
    public string PluginName { get; private set; }
    public Command InvokeCommand { get; private set;} 

    public PluginViewModel(ClientPlugin plugin)
    {
      Header = plugin.MenuCaption;
      PluginName = plugin.Name;
      InvokeCommand = new Command(Invoke);
    } 

    public void Invoke(object o)
    {
      var plugin = ClientModel.Plugins.GetPlugin(PluginName);
      if (plugin != null)
        plugin.InvokeMenuHandler();
    }
  }
}
