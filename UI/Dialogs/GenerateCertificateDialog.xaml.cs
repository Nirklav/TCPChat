using Engine.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UI.Infrastructure;

namespace UI.Dialogs
{
  public partial class GenerateCertificateDialog : Window
  {
    private readonly string _subjectFormat;

    public string CertificatePath { get; private set; }

    public GenerateCertificateDialog(string subjectName, string subjectFormat)
    {
      InitializeComponent();

      SubjectLabel.Content = subjectName;
      _subjectFormat = subjectFormat;
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        if (string.IsNullOrEmpty(SubjectField.Text))
          throw new FormatException();

        if (string.IsNullOrEmpty(CertificateField.Text))
          throw new FormatException();

        CertificatePath = CertificateField.Text;

        var subject = "CN=" + string.Format(_subjectFormat, SubjectField.Text);
        var from = DateTime.UtcNow;
        var to = DateTime.UtcNow.AddYears(4);
        var certBlob = CertificateGenerator.CreateSelfSignedPfx(subject, from, to, PasswordField.SecurePassword);
        File.WriteAllBytes(CertificateField.Text, certBlob);

        DialogResult = true;
      }
      catch (FormatException)
      {
        MessageBox.Show(this, Localizer.Instance.Localize("fieldsError"), "TCP Chat");
      }
    }

    private void SelectCertificatePath_Click(object sender, RoutedEventArgs e)
    {
      var fileDialog = new SaveFileDialog();
      fileDialog.CheckPathExists = true;
      fileDialog.Filter = "PFX|*.pfx";
      if (fileDialog.ShowDialog() == true)
        CertificateField.Text = fileDialog.FileName;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }
  }
}
