using System;
using System.Drawing;
using System.Windows;
using UI.Infrastructure;

namespace UI.Dialogs
{
  public partial class ServerDialog : Window
  {
    public ServerDialog()
    {
      InitializeComponent();

      NickField.Text = Settings.Current.Nick;

      var colorRandom = new Random();
      RedColorSlider.Value = Settings.Current.RandomColor ? colorRandom.Next(30, 170) : Settings.Current.NickColor.R;
      GreenColorSlider.Value = Settings.Current.RandomColor ? colorRandom.Next(30, 170) : Settings.Current.NickColor.G;
      BlueColorSlider.Value = Settings.Current.RandomColor ? colorRandom.Next(30, 170) : Settings.Current.NickColor.B;

      PortField.Text = Settings.Current.ServerPort.ToString();

      UsingIPv6RadBtn.IsChecked = Settings.Current.ServerUseIpv6;
      UsingIPv4RadBtn.IsChecked = !Settings.Current.ServerUseIpv6;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
      if (NickField.Text == String.Empty || PortField.Text == String.Empty)
      {
        MessageBox.Show(this, "Проверьте правильность заполнения всех полей.", "TCP Chat");
        return;
      }

      Settings.Current.Nick = NickField.Text;
      Settings.Current.NickColor = Color.FromArgb((int)RedColorSlider.Value, (int)GreenColorSlider.Value, (int)BlueColorSlider.Value);
      Settings.Current.ServerUseIpv6 = UsingIPv6RadBtn.IsChecked == true;
      Settings.Current.RandomColor = false;

      try
      {
        int port = int.Parse(PortField.Text);

        if (port > ushort.MaxValue)
          throw new FormatException();

        Settings.Current.ServerPort = port;
      }
      catch (FormatException)
      {
        MessageBox.Show(this, "Проверьте правильность заполнения всех полей.", "TCP Chat");
        return;
      }

      DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }
  }
}
