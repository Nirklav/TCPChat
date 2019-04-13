using Engine.Network;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Security;
using System.Windows;
using UI.Infrastructure;

namespace UI.Dialogs
{
  public partial class ConnectDialog : Window
  {
    public string Nick { get; private set; }
    public Color NickColor { get; private set; }
    public string Address { get; private set; }
    public string CertificatePath { get; private set; }
    public SecureString CertificatePassword { get; private set; }

    public ConnectDialog()
    {
      InitializeComponent();

      Nick = NickField.Text = Settings.Current.Nick;
      Address = ServerAddressField.Text = Settings.Current.ServerAddress;
      CertificatePath = CertificateField.Text = Settings.Current.CertificatePath;

      if (Settings.Current.RandomColor)
        NickColor = GetRandomColor();
      else
        NickColor = Settings.Current.NickColor;

      RedColorSlider.Value = NickColor.R;
      GreenColorSlider.Value = NickColor.G;
      BlueColorSlider.Value = NickColor.B;
    }

    public void SaveSettings()
    {
      Settings.Current.Nick = Nick;
      Settings.Current.NickColor = NickColor;
      Settings.Current.ServerAddress = Address;
      Settings.Current.CertificatePath = CertificatePath;
      Settings.Current.RandomColor = false;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        if (string.IsNullOrEmpty(NickField.Text))
          throw new FormatException("Nick field is empty.");

        if (string.IsNullOrEmpty(ServerAddressField.Text))
          throw new FormatException("Server address field is empty.");

        Nick = NickField.Text;
        NickColor = Color.FromArgb((int)RedColorSlider.Value, (int)GreenColorSlider.Value, (int)BlueColorSlider.Value);
        CertificatePath = CertificateField.Text;
        CertificatePassword = PasswordField.SecurePassword;
      
        var uri = Connection.CreateTcpchatUri(ServerAddressField.Text);

        switch (uri.HostNameType)
        {
          case UriHostNameType.Dns:
          case UriHostNameType.IPv4:
          case UriHostNameType.IPv6:
            Address = ServerAddressField.Text;
            break;
          default:
            throw new FormatException("Unknown host name type. Allowed: Dns, IPv4 or IPv6.");
        }

        DialogResult = true;
      }
      catch (FormatException fe)
      {
        var error = Localizer.Instance.Localize("fieldsError");
        MessageBox.Show(this, $"{error}\r\n{fe.Message}", "TCP Chat");
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
      var generateCertificate = GenerateCertificateDialog.ForNick();
      if (generateCertificate.ShowDialog() == true)
        CertificateField.Text = generateCertificate.CertificatePath;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }

    private static Color GetRandomColor()
    {
      var colorRandom = new Random();
      var r = colorRandom.Next(30, 170);
      var g = colorRandom.Next(30, 170);
      var b = colorRandom.Next(30, 170);
      return Color.FromArgb(r, g, b);
    }
  }
}
