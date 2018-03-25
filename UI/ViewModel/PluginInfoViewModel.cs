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
    private string _name;
    private bool _enabled;

    public string Name
    {
      get { return _name; }
      set { SetValue(value, nameof(Name), v => _name = v); }
    }

    public bool Enabled
    {
      get { return _enabled; }
      set { SetValue(value, nameof(Enabled), v => _enabled = v); }
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
