using Microsoft.Win32;
using System.Windows.Input;
using UI.Dialogs;
using UI.Infrastructure;

namespace UI.ViewModel
{
  public class ServerTabViewModel : SettingsTabViewModel
  {
    private const string NameKey = "settingsTabCategory-server";

    #region fields
    private string _serverAddress;
    private string _certificatePath;
    private int _servicePort;
    private bool _defaultServicePort;
    #endregion

    #region properties
    public string ServerAddress
    {
      get { return _serverAddress; }
      set { SetValue(value, nameof(ServerAddress), v => _serverAddress = v); }
    }

    public string CertificatePath
    {
      get { return _certificatePath; }
      set { SetValue(value, nameof(CertificatePath), v => _certificatePath = v); }
    }

    public bool DefaultSevicePort
    {
      get { return _defaultServicePort; }
      set
      {
        SetValue(value, nameof(DefaultSevicePort), v => _defaultServicePort = v);

        if (value == true)
          SetValue(0, nameof(ServicePort), v => _servicePort = v);
      }
    }

    public int ServicePort
    {
      get { return _servicePort; }
      set { SetValue(value, nameof(ServicePort), v => _servicePort = v); }
    }
    #endregion

    #region commands
    public ICommand SelectCertificateCommand { get; private set; }
    public ICommand GenerateCertificateCommand { get; private set; }
    #endregion

    public ServerTabViewModel() 
      : base(NameKey, SettingsTabCategory.Server)
    {
      SelectCertificateCommand = new Command(SelectCertificate);
      GenerateCertificateCommand = new Command(GenerateCertificate);

      ServerAddress = Settings.Current.ServerStartAddress;
      CertificatePath = Settings.Current.ServerStartCertificatePath;
      ServicePort = Settings.Current.ServerStartP2PPort;
      DefaultSevicePort = Settings.Current.ServerStartP2PPort == 0;
    }

    public override void SaveSettings()
    {
      Settings.Current.ServerStartAddress = ServerAddress;
      Settings.Current.ServerStartCertificatePath = CertificatePath;
      Settings.Current.ServerStartP2PPort = DefaultSevicePort ? 0 : ServicePort;
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
      var generateCertificate = GenerateCertificateDialog.ForServerAddress();
      if (generateCertificate.ShowDialog() == true)
        CertificatePath = generateCertificate.CertificatePath;
    }
  }
}
