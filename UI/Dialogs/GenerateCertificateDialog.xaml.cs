using Engine.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
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
    public const string TcpChatNickPrefix = "TcpChat:nick:";

    private readonly string _subjectFormat;

    public string CertificatePath { get; private set; }

    private GenerateCertificateDialog(string subjectName, string subjectFormat)
    {
      InitializeComponent();

      SubjectLabel.Content = subjectName;
      _subjectFormat = subjectFormat;
    }

    public static GenerateCertificateDialog ForServerAddress()
    {
      return new GenerateCertificateDialog(Localizer.Instance.Localize("serverAddress"), "{0}");
    }

    public static GenerateCertificateDialog ForNick()
    {
      return new GenerateCertificateDialog(Localizer.Instance.Localize("nick"), TcpChatNickPrefix + "{0}");
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        if (string.IsNullOrEmpty(SubjectField.Text))
          throw new FormatException();

        if (string.IsNullOrEmpty(CertificateField.Text))
          throw new FormatException();

        var pass = PasswordField.SecurePassword;
        var checkPass = CheckPasswordField.SecurePassword;

        if (!Equals(pass, checkPass))
          throw new FormatException();

        CertificatePath = CertificateField.Text;

        var subject = "CN=" + string.Format(_subjectFormat, SubjectField.Text);
        var from = DateTime.UtcNow;
        var to = DateTime.UtcNow.AddYears(4);
        var certBlob = CertificateGenerator.CreateSelfSignedPfx(subject, from, to, pass);
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

    private bool Equals(SecureString firstPassword, SecureString secondPassword)
    {
      using (var clearFirst = new ClearSecureString(firstPassword))
      using (var clearSecond = new ClearSecureString(secondPassword))
      {
        if (clearFirst.Bytes.Length != clearSecond.Bytes.Length)
          return false;

        var length = clearFirst.Bytes.Length;
        for (int i = 0; i < length; i++)
        {
          var f = clearFirst.Bytes[i];
          var s = clearSecond.Bytes[i];

          if (f != s)
            return false;
        }
        return true;
      }
    }

    private struct ClearSecureString : IDisposable
    {
      private readonly SecureString _secureString;
      private readonly IntPtr _unmanaged;
      private readonly byte[] _clear;

      public ClearSecureString(SecureString secureString)
      {
        _secureString = secureString;
        _unmanaged = Marshal.SecureStringToGlobalAllocUnicode(secureString);
        _clear = new byte[_secureString.Length * sizeof(char)];

        Marshal.Copy(_unmanaged, _clear, 0, _clear.Length);
      }

      public byte[] Bytes {  get { return _clear; } }

      public void Dispose()
      {
        if (_unmanaged != IntPtr.Zero)
          Marshal.ZeroFreeGlobalAllocUnicode(_unmanaged);

        if (_clear != null)
        {
          for (int i = 0; i < _clear.Length; i++)
            _clear[i] = 0;
        }
      }
    }
  }
}
