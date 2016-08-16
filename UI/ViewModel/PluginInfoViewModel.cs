using UI.Infrastructure;

namespace UI.ViewModel
{
  public enum PluginKindId
  {
    Server,
    Client,
  }

  public class PluginInfoViewModel : BaseViewModel
  {
    private string name;
    private bool enabled;

    public string Name
    {
      get { return name; }
      set { SetValue(value, "Name", v => name = v); }
    }

    public bool Enabled
    {
      get { return enabled; }
      set { SetValue(value, "Enabled", v => enabled = v); }
    }

    public PluginKindId Kind { get; private set; }

    public PluginInfoViewModel(PluginSetting setting, PluginKindId kindId)
      : base(null, false)
    {
      Name = setting.Name;
      Enabled = setting.Enabled;
      Kind = kindId;
    }

    public PluginSetting ToSetting()
    {
      return new PluginSetting
      {
        Name = Name,
        Enabled = Enabled
      };
    }
  }
}
