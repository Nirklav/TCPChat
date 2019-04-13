using Microsoft.Win32;
using System;
using System.IO;
using System.Security;
using System.Windows;
using UI.Infrastructure;

namespace UI.Dialogs
{
  public partial class ServerDialog : Window
  {
    public string ServerAddress { get; private set; }
    public string CertificatePath { get; private set; }
    public SecureString CertificatePassword { get; private set; }

    public ServerDialog()
    {
      InitializeComponent();

      ServerAddress = ServerAddressField.Text = Settings.Current.ServerStartAddress;
      CertificatePath = CertificateField.Text = Settings.Current.ServerStartCertificatePath;
    }

    public void SaveSettings()
    {
      Settings.Current.ServerStartAddress = ServerAddress;
      Settings.Current.ServerStartCertificatePath = CertificatePath;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        if (string.IsNullOrEmpty(ServerAddressField.Text))
          throw new FormatException("Server address field is empty.");

        if (string.IsNullOrEmpty(CertificateField.Text))
          throw new FormatException("Certificate field is empty.");

        if (!File.Exists(CertificateField.Text))
          throw new FormatException("Certificate file does not exist.");

        ServerAddress = ServerAddressField.Text;
        CertificatePath = CertificateField.Text;
        CertificatePassword = PasswordField.SecurePassword;

        DialogResult = true;
      }
      catch (FormatException fe)
      {
        var errorMsg = Localizer.Instance.Localize("fieldsError");
        MessageBox.Show(this, $"{errorMsg}\r\n{fe.Message}", "TCP Chat");
      }
    }

    private void SelectCertificatePath_Click(object sender, RoutedEventArgs e)
    {
      var fileDialog = new OpenFileDialog();
      fileDialog.CheckFileExists = true;
      fileDialog.CheckPathExists = true;
      fileDialog.Multiselect = false;
      fileDialog.Filter = "PFX|*.pfx|All files|*.*";
      if (fileDialog.ShowDialog() == true)
        CertificateField.Text = fileDialog.FileName;
    }

    private void GenerateCertificate_Click(object sender, RoutedEventArgs e)
    {
      var generateCertificate = GenerateCertificateDialog.ForServerAddress();
      if (generateCertificate.ShowDialog() == true)
        CertificateField.Text = generateCertificate.CertificatePath;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }
  }
}
