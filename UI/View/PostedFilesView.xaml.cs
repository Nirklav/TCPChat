using System.Windows;
using System.Windows.Controls;

namespace UI.Dialogs
{
  /// <summary>
  /// Логика взаимодействия для FilesWindow.xaml
  /// </summary>
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
