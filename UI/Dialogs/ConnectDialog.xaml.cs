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
      AddressField.Text = Settings.Current.Address;

      Random colorRandom = new Random();
      RedColorSlider.Value = Settings.Current.RandomColor ? colorRandom.Next(30, 170) : Settings.Current.NickColor.R;
      GreenColorSlider.Value = Settings.Current.RandomColor ? colorRandom.Next(30, 170) : Settings.Current.NickColor.G;
      BlueColorSlider.Value = Settings.Current.RandomColor ? colorRandom.Next(30, 170) : Settings.Current.NickColor.B;

      PortField.Text = Settings.Current.Port.ToString();
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrEmpty(NickField.Text) || string.IsNullOrEmpty(PortField.Text) || string.IsNullOrEmpty(AddressField.Text))
      {
        MessageBox.Show(this, "Проверьте правильность заполнения всех полей.", "TCP Chat");
        return;
      }

      Settings.Current.Nick = NickField.Text;
      Settings.Current.NickColor = Color.FromArgb((int)RedColorSlider.Value, (int)GreenColorSlider.Value, (int)BlueColorSlider.Value);
      Settings.Current.RandomColor = false;

      try
      {
        IPAddress.Parse(AddressField.Text); // Валидация
        int port = int.Parse(PortField.Text);

        if (port > ushort.MaxValue)
          throw new FormatException();

        Settings.Current.Address = AddressField.Text;
        Settings.Current.Port = port;
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
