using System.Windows;
using System.Windows.Controls;

namespace UI.Dialogs
{
  public partial class PostedFilesView : Window
  {
    public PostedFilesView()
    {
      InitializeComponent();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
    }
  }
}
