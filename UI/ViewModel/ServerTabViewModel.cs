using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UI.Infrastructure;

namespace UI.ViewModel
{
  public class ServerTabViewModel : SettingsTabViewModel
  {
    private const string NameKey = "settingsTabCategory-server";

    #region fields
    private int serverPort;
    private int servicePort;
    private bool defaultServicePort;
    private bool enabledIPv6;
    private bool enabledIPv4;
    #endregion

    #region properties
    public int ServerPort
    {
      get { return serverPort; }
      set { SetValue(value, "ServerPort", v => serverPort = v); }
    }

    public bool DefaultSevicePort
    {
      get { return defaultServicePort; }
      set
      {
        SetValue(value, "DefaultSevicePort", v => defaultServicePort = v);

        if (value == true)
          SetValue(0, "ServicePort", v => servicePort = v);
      }
    }

    public int ServicePort
    {
      get { return servicePort; }
      set { SetValue(value, "ServicePort", v => servicePort = v); }
    }

    public bool IPv4Enabled
    {
      get { return enabledIPv4; }
      set { SetValue(value, "IPv4Enabled", v => enabledIPv4 = v); }
    }

    public bool IPv6Enabled
    {
      get { return enabledIPv6; }
      set { SetValue(value, "IPv6Enabled", v => enabledIPv6 = v); }
    }
    #endregion

    public ServerTabViewModel() 
      : base(NameKey, SettingsTabCategory.Server)
    {
      ServerPort = Settings.Current.Port;
      ServicePort = Settings.Current.ServicePort;
      DefaultSevicePort = Settings.Current.ServicePort == 0;

      IPv4Enabled = !Settings.Current.StateOfIPv6Protocol;
      IPv6Enabled = Settings.Current.StateOfIPv6Protocol;
    }

    public override void SaveSettings()
    {
      Settings.Current.Port = ServerPort;
      Settings.Current.ServicePort = DefaultSevicePort ? 0 : ServicePort;
      Settings.Current.StateOfIPv6Protocol = IPv6Enabled;
    }
  }
}
