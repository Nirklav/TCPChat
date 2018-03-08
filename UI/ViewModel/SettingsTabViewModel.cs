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
    private string _nameKey;
    private SettingsTabCategory _category;
    #endregion

    #region properties
    public SettingsTabCategory Category
    {
      get { return _category; }
      set { SetValue(value, "Category", v => _category = v); }
    }

    public string Name
    {
      get { return Localizer.Instance.Localize(_nameKey); }
    }
    #endregion

    public SettingsTabViewModel(string nameKey, SettingsTabCategory category)
      : base(null, false)
    {
      _nameKey = nameKey;
      _category = category;
    }

    public abstract void SaveSettings();  
  }
}
