using Microsoft.Win32;
using System.Drawing;
using System.Windows.Input;
using UI.Dialogs;
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
    private string _certificatePath;

    private byte _redValue;
    private byte _greenValue;
    private byte _blueValue;

    private string _adminPassword;
    #endregion

    #region properties
    public string Locale
    {
      get { return _locale; }
      set { SetValue(value, nameof(Locale), v => _locale = v); }
    }

    public string[] Locales
    {
      get { return LocalizerStorage.Languages; }
    }

    public string Nick
    {
      get { return _nick; }
      set { SetValue(value, nameof(Nick), v => _nick = v); }
    }

    public string CertificatePath
    {
      get { return _certificatePath; }
      set { SetValue(value, nameof(CertificatePath), v => _certificatePath = v); }
    }

    public WPFColor NickColor
    {
      get { return WPFColor.FromRgb(RedValue, GreenValue, BlueValue); }
    }

    public byte RedValue
    {
      get { return _redValue; }
      set { SetValue(value, nameof(NickColor), v => _redValue = v); }
    }

    public byte GreenValue
    {
      get { return _greenValue; }
      set { SetValue(value, nameof(NickColor), v => _greenValue = v); }
    }

    public byte BlueValue
    {
      get { return _blueValue; }
      set { SetValue(value, nameof(NickColor), v => _blueValue = v); }
    }

    public string AdminPassword
    {
      get { return _adminPassword; }
      set { SetValue(value, nameof(AdminPassword), v => _adminPassword = v); }
    }
    #endregion

    #region commands
    public ICommand SelectCertificateCommand { get; private set; }
    public ICommand GenerateCertificateCommand { get; private set; }
    #endregion

    public ClientTabViewModel() 
      : base(NameKey, SettingsTabCategory.Client)
    {
      SelectCertificateCommand = new Command(SelectCertificate);
      GenerateCertificateCommand = new Command(GenerateCertificate);

      Locale = Settings.Current.Locale;
      Nick = Settings.Current.Nick;
      CertificatePath = Settings.Current.CertificatePath;

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
      Settings.Current.CertificatePath = CertificatePath;
      Settings.Current.NickColor = Color.FromArgb(RedValue, GreenValue, BlueValue);
      Settings.Current.AdminPassword = _adminPassword;
    }

    private void SelectCertificate(object obj)
    {
      var fileDialog = new OpenFileDialog();
      fileDialog.CheckFileExists = true;
      fileDialog.CheckPathExists = true;
      fileDialog.Multiselect = false;
      fileDialog.Filter = "PFX|*.pfx|All files|*.*";

      if (fileDialog.ShowDialog() == true)
        CertificatePath = fileDialog.FileName;
    }

    private void GenerateCertificate(object obj)
    {
      var generateCertificate = GenerateCertificateDialog.ForNick();
      if (generateCertificate.ShowDialog() == true)
        CertificatePath = generateCertificate.CertificatePath;
    }
  }
}
