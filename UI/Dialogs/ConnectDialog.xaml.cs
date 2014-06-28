using System;
using System.Drawing;
using System.Net;
using System.Windows;

namespace UI.Dialogs
{
  /// <summary>
  /// Логика взаимодействия для ConnectDialog.xaml
  /// </summary>
  public partial class ConnectDialog : Window
  {
    public string Nick { get; set; }
    public IPAddress Address { get; set; }
    public Color NickColor { get; set; }
    public int Port { get; set; }

    public ConnectDialog(string nick, Color nickColor, string address, int port)
    {
      InitializeComponent();

      NickField.Text = nick;
      AddressField.Text = address;

      Random colorRandom = new Random();

      bool random = false;
      if (Color.Equals(nickColor, Color.FromArgb(170, 50, 50)))
        random = true;

      RedColorSlider.Value = random ? colorRandom.Next(30, 170) : nickColor.R;
      GreenColorSlider.Value = random ? colorRandom.Next(30, 170) : nickColor.G;
      BlueColorSlider.Value = random ? colorRandom.Next(30, 170) : nickColor.B;

      PortField.Text = port.ToString();
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrEmpty(NickField.Text) || string.IsNullOrEmpty(PortField.Text) || string.IsNullOrEmpty(AddressField.Text))
      {
        MessageBox.Show(this, "Проверьте правильность заполнения всех полей.", "TCP Chat");
        return;
      }

      Nick = NickField.Text;
      NickColor = Color.FromArgb((int)RedColorSlider.Value, (int)GreenColorSlider.Value, (int)BlueColorSlider.Value);

      try
      {
        Address = IPAddress.Parse(AddressField.Text);
        Port = int.Parse(PortField.Text);

        if (Port > ushort.MaxValue)
          throw new FormatException();
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
