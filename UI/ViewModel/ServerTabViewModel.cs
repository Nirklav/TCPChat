using UI.Infrastructure;

namespace UI.ViewModel
{
  public class ServerTabViewModel : SettingsTabViewModel
  {
    private const string NameKey = "settingsTabCategory-server";

    #region fields
    private string _serverAddress;
    private int _servicePort;
    private bool _defaultServicePort;
    private bool _enabledIPv6;
    private bool _enabledIPv4;
    #endregion

    #region properties
    public string ServerAddress
    {
      get { return _serverAddress; }
      set { SetValue(value, nameof(ServerAddress), v => _serverAddress = v); }
    }

    public bool DefaultSevicePort
    {
      get { return _defaultServicePort; }
      set
      {
        SetValue(value, nameof(DefaultSevicePort), v => _defaultServicePort = v);

        if (value == true)
          SetValue(0, nameof(ServicePort), v => _servicePort = v);
      }
    }

    public int ServicePort
    {
      get { return _servicePort; }
      set { SetValue(value, nameof(ServicePort), v => _servicePort = v); }
    }

    public bool IPv4Enabled
    {
      get { return _enabledIPv4; }
      set { SetValue(value, nameof(IPv4Enabled), v => _enabledIPv4 = v); }
    }

    public bool IPv6Enabled
    {
      get { return _enabledIPv6; }
      set { SetValue(value, nameof(IPv6Enabled), v => _enabledIPv6 = v); }
    }
    #endregion

    public ServerTabViewModel() 
      : base(NameKey, SettingsTabCategory.Server)
    {
      ServerAddress = Settings.Current.ServerStartAddress;
      ServicePort = Settings.Current.ServerStartP2PPort;
      DefaultSevicePort = Settings.Current.ServerStartP2PPort == 0;
    }

    public override void SaveSettings()
    {
      Settings.Current.ServerStartAddress = ServerAddress;
      Settings.Current.ServerStartP2PPort = DefaultSevicePort ? 0 : ServicePort;
    }
  }
}
