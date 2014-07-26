using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace UI.ViewModel
{
  public abstract class SettingsTabViewModel : BaseViewModel
  {
    #region fields
    private string name;
    #endregion

    #region properties
    public string Name
    {
      get { return name; }
      set { SetValue(value, "Name", v => name = v); }
    }
    #endregion

    public SettingsTabViewModel(string tabName)
    {
      Name = tabName;
    }

    public abstract void SaveSettings();  
  }
}
