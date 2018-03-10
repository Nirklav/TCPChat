using System;
using System.Drawing;
using System.Net;
using System.Windows;
using UI.Infrastructure;

namespace UI.Dialogs
{
  public partial class ConnectDialog : Window
  {
    public ConnectDialog()
    {
      InitializeComponent();

      NickField.Text = Settings.Current.Nick;
      UriField.Text = Settings.Current.Uri;

      var colorRandom = new Random();
      RedColorSlider.Value = Settings.Current.RandomColor ? colorRandom.Next(30, 170) : Settings.Current.NickColor.R;
      GreenColorSlider.Value = Settings.Current.RandomColor ? colorRandom.Next(30, 170) : Settings.Current.NickColor.G;
      BlueColorSlider.Value = Settings.Current.RandomColor ? colorRandom.Next(30, 170) : Settings.Current.NickColor.B;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        if (string.IsNullOrEmpty(NickField.Text) || string.IsNullOrEmpty(UriField.Text))
          throw new FormatException();

        Settings.Current.Nick = NickField.Text;
        Settings.Current.NickColor = Color.FromArgb((int)RedColorSlider.Value, (int)GreenColorSlider.Value, (int)BlueColorSlider.Value);
        Settings.Current.RandomColor = false;

        var uri = new Uri(UriField.Text);

        switch (uri.HostNameType)
        {
          case UriHostNameType.Dns:
          case UriHostNameType.IPv4:
          case UriHostNameType.IPv6:
            Settings.Current.Uri = UriField.Text;
            DialogResult = true;
            break;
          default:
            throw new FormatException();
        }
      }
      catch (FormatException)
      {
        MessageBox.Show(this, Localizer.Instance.Localize("connectDialog-fieldsError"), "TCP Chat");
      }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }
  }
}
