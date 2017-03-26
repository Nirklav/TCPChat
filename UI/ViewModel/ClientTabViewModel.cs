using System.Drawing;
using UI.Infrastructure;
using WPFColor = System.Windows.Media.Color;

namespace UI.ViewModel
{
  public class ClientTabViewModel : SettingsTabViewModel
  {
    private const string NameKey = "settingsTabCategory-client";

    #region fields
    private string locale;

    private string nick;

    private byte redValue;
    private byte greenValue;
    private byte blueValue;

    private string adminPassword;
    #endregion

    #region properties
    public string Locale
    {
      get { return locale; }
      set { SetValue(value, "Locale", v => locale = v); }
    }

    public string[] Locales
    {
      get { return LocalizerStorage.Languages; }
    }

    public string Nick
    {
      get { return nick; }
      set { SetValue(value, "Nick", v => nick = v); }
    }

    public WPFColor NickColor
    {
      get { return WPFColor.FromRgb(RedValue, GreenValue, BlueValue); }
    }

    public byte RedValue
    {
      get { return redValue; }
      set { SetValue(value, "NickColor", v => redValue = v); }
    }

    public byte GreenValue
    {
      get { return greenValue; }
      set { SetValue(value, "NickColor", v => greenValue = v); }
    }

    public byte BlueValue
    {
      get { return blueValue; }
      set { SetValue(value, "NickColor", v => blueValue = v); }
    }

    public string AdminPassword
    {
      get { return adminPassword; }
      set { SetValue(value, "AdminPassword", v => adminPassword = v); }
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
      Localizer.Instance.Set(locale);

      Settings.Current.Locale = Locale;
      Settings.Current.Nick = Nick;
      Settings.Current.NickColor = Color.FromArgb(RedValue, GreenValue, BlueValue);
      Settings.Current.AdminPassword = adminPassword;
    }
  }
}
