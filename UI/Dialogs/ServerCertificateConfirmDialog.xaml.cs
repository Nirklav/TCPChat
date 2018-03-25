using Engine.Model.Client;
using System.Security.Cryptography.X509Certificates;
using System.Windows;

namespace UI.View
{
  public partial class ServerCertificateConfirmDialog : Window
  {
    private readonly X509Certificate2 _certificate;

    public ServerCertificateConfirmDialog(X509Certificate2 certificate)
    {
      _certificate = certificate;

      InitializeComponent();
    }

    private void Show_Click(object sender, RoutedEventArgs e)
    {
      X509Certificate2UI.DisplayCertificate(_certificate);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
      ClientModel.TrustedCertificates.Add(_certificate);
      DialogResult = true;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }
  }
}
