using UI.Infrastructure;

namespace UI.ViewModel
{
  public class ServerTabViewModel : SettingsTabViewModel
  {
    private const string NameKey = "settingsTabCategory-server";

    #region fields
    private int _serverPort;
    private int _servicePort;
    private bool _defaultServicePort;
    private bool _enabledIPv6;
    private bool _enabledIPv4;
    #endregion

    #region properties
    public int ServerPort
    {
      get { return _serverPort; }
      set { SetValue(value, "ServerPort", v => _serverPort = v); }
    }

    public bool DefaultSevicePort
    {
      get { return _defaultServicePort; }
      set
      {
        SetValue(value, "DefaultSevicePort", v => _defaultServicePort = v);

        if (value == true)
          SetValue(0, "ServicePort", v => _servicePort = v);
      }
    }

    public int ServicePort
    {
      get { return _servicePort; }
      set { SetValue(value, "ServicePort", v => _servicePort = v); }
    }

    public bool IPv4Enabled
    {
      get { return _enabledIPv4; }
      set { SetValue(value, "IPv4Enabled", v => _enabledIPv4 = v); }
    }

    public bool IPv6Enabled
    {
      get { return _enabledIPv6; }
      set { SetValue(value, "IPv6Enabled", v => _enabledIPv6 = v); }
    }
    #endregion

    public ServerTabViewModel() 
      : base(NameKey, SettingsTabCategory.Server)
    {
      ServerPort = Settings.Current.ServerPort;
      ServicePort = Settings.Current.ServerP2PPort;
      DefaultSevicePort = Settings.Current.ServerP2PPort == 0;

      IPv4Enabled = !Settings.Current.ServerUseIpv6;
      IPv6Enabled = Settings.Current.ServerUseIpv6;
    }

    public override void SaveSettings()
    {
      Settings.Current.ServerPort = ServerPort;
      Settings.Current.ServerP2PPort = DefaultSevicePort ? 0 : ServicePort;
      Settings.Current.ServerUseIpv6 = IPv6Enabled;
    }
  }
}
