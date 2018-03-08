using System.Drawing;
using UI.Infrastructure;
using WPFColor = System.Windows.Media.Color;

namespace UI.ViewModel
{
  public class ClientTabViewModel : SettingsTabViewModel
  {
    private const string NameKey = "settingsTabCategory-client";

    #region fields
    private string _locale;

    private string _nick;

    private byte _redValue;
    private byte _greenValue;
    private byte _blueValue;

    private string _adminPassword;
    #endregion

    #region properties
    public string Locale
    {
      get { return _locale; }
      set { SetValue(value, "Locale", v => _locale = v); }
    }

    public string[] Locales
    {
      get { return LocalizerStorage.Languages; }
    }

    public string Nick
    {
      get { return _nick; }
      set { SetValue(value, "Nick", v => _nick = v); }
    }

    public WPFColor NickColor
    {
      get { return WPFColor.FromRgb(RedValue, GreenValue, BlueValue); }
    }

    public byte RedValue
    {
      get { return _redValue; }
      set { SetValue(value, "NickColor", v => _redValue = v); }
    }

    public byte GreenValue
    {
      get { return _greenValue; }
      set { SetValue(value, "NickColor", v => _greenValue = v); }
    }

    public byte BlueValue
    {
      get { return _blueValue; }
      set { SetValue(value, "NickColor", v => _blueValue = v); }
    }

    public string AdminPassword
    {
      get { return _adminPassword; }
      set { SetValue(value, "AdminPassword", v => _adminPassword = v); }
    }

    #endregion

    public ClientTabViewModel() 
      : base(NameKey, SettingsTabCategory.Client)
    {
      Locale = Settings.Current.Locale;

      Nick = Settings.Current.Nick;

      RedValue = Settings.Current.NickColor.R;
      GreenValue = Settings.Current.NickColor.G;
      BlueValue = Settings.Current.NickColor.B;

      AdminPassword = Settings.Current.AdminPassword;
    }

    public override void SaveSettings()
    {
      Localizer.Instance.Set(_locale);

      Settings.Current.Locale = Locale;
      Settings.Current.Nick = Nick;
      Settings.Current.NickColor = Color.FromArgb(RedValue, GreenValue, BlueValue);
      Settings.Current.AdminPassword = _adminPassword;
    }
  }
}
