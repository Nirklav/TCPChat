using System.Windows;

namespace UI.Dialogs
{
  public partial class AboutProgramDialog : Window
  {
    public AboutProgramDialog()
    {
      InitializeComponent();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
    }
  }
}
