using System;
using System.Drawing;
using System.Windows;

namespace UI.Dialogs
{
  /// <summary>
  /// Логика взаимодействия для ServerDialog.xaml
  /// </summary>
  public partial class ServerDialog : Window
  {
    public bool UsingIPv6Protocol { get; set; }
    public string Nick { get; set; }
    public Color NickColor { get; set; }
    public int Port { get; set; }

    public ServerDialog(string nick, Color nickColor, int port, bool usingIPv6)
    {
      InitializeComponent();

      NickField.Text = nick;

      Random colorRandom = new Random();

      bool random = false;
      if (Color.Equals(nickColor, Color.FromArgb(170, 50, 50)))
        random = true;

      RedColorSlider.Value = random ? colorRandom.Next(30, 170) : nickColor.R;
      GreenColorSlider.Value = random ? colorRandom.Next(30, 170) : nickColor.G;
      BlueColorSlider.Value = random ? colorRandom.Next(30, 170) : nickColor.B;

      PortField.Text = port.ToString();

      UsingIPv6RadBtn.IsChecked = usingIPv6;
      UsingIPv4RadBtn.IsChecked = !usingIPv6;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
      if (NickField.Text == String.Empty || PortField.Text == String.Empty)
      {
        MessageBox.Show(this, "Проверьте правильность заполнения всех полей.", "TCP Chat");
        return;
      }

      Nick = NickField.Text;
      NickColor = Color.FromArgb((int)RedColorSlider.Value, (int)GreenColorSlider.Value, (int)BlueColorSlider.Value);

      if (UsingIPv6RadBtn.IsChecked != null && UsingIPv6RadBtn.IsChecked != false)
        UsingIPv6Protocol = true;
      else
        UsingIPv6Protocol = false;

      try
      {
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
