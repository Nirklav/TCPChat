using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using UI.Infrastructure;

namespace UI.ViewModel
{
  public enum SettingsTabCategory
  {
    Client,
    Server,
    Audio,
    Plugins
  }

  public abstract class SettingsTabViewModel : BaseViewModel
  {
    #region fields
    private string nameKey;
    private SettingsTabCategory category;
    #endregion

    #region properties
    public SettingsTabCategory Category
    {
      get { return category; }
      set { SetValue(value, "Category", v => category = v); }
    }

    public string Name
    {
      get { return Localizer.Instance.Localize(nameKey); }
    }
    #endregion

    public SettingsTabViewModel(string nameKey, SettingsTabCategory category)
      : base(false)
    {
      this.nameKey = nameKey;
      this.category = category;
    }

    public abstract void SaveSettings();  
  }
}
