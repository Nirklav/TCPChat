using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UI.Infrastructure;

namespace UI.ViewModel
{
  public class CommonTabViewModel : SettingsTabViewModel
  {
    #region fields
    private int serverPort;
    private int servicePort;
    private bool defaultServicePort;
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
    #endregion

    public CommonTabViewModel(string name) : base(name)
    {
      ServerPort = Settings.Current.Port;
      ServicePort = Settings.Current.ServicePort;
      DefaultSevicePort = Settings.Current.ServicePort == 0;
    }

    public override void SaveSettings()
    {
      Settings.Current.Port = ServerPort;
      Settings.Current.ServicePort = DefaultSevicePort ? 0 : ServicePort;
    }
  }
}
