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
    private int srvicePort;
    #endregion

    #region properties
    public int ServerPort
    {
      get { return serverPort; }
      set { SetValue(value, "ServerPort", v => serverPort = v); }
    }

    public int ServicePort
    {
      get { return srvicePort; }
      set { SetValue(value, "ServicePort", v => srvicePort = v); }
    }
    #endregion

    public CommonTabViewModel(string name) : base(name)
    {
      ServerPort = Settings.Current.Port;
      ServicePort = Settings.Current.ServicePort;
    }

    public override void SaveSettings()
    {
      Settings.Current.Port = ServerPort;
      Settings.Current.ServicePort = ServicePort;
    }
  }
}
