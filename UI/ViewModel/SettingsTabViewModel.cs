using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UI.ViewModel
{
  public class SettingsTabViewModel : BaseViewModel
  {
    private string name;

    public string Name
    {
      get { return name; }
      set
      {
        name = value;
        OnPropertyChanged("Name");
      }
    }

    public SettingsTabViewModel(string tabName)
    {
      Name = tabName;
    }

    public virtual void SaveSettings() { }  
  }
}
