using Engine.Model.Client;
using Engine.Model.Entities;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UI.Dialogs
{
  /// <summary>
  /// Логика взаимодействия для FilesWindow.xaml
  /// </summary>
  public partial class PostedFilesDialog : Window
  {
    class Container
    {
      public PostedFile PostedFile { get; set; }
    }

    public PostedFilesDialog()
    {
      InitializeComponent();

      RefreshFiles();
    }

    private void RefreshFiles()
    {
      files.Items.Clear();

      DataTemplate postedFileTemplate = (DataTemplate)FindResource("PostedFileTemplate");

      using (var client = ClientModel.Get())
      {
        foreach (PostedFile current in client.PostedFiles)
        {
          TreeViewItem roomItem = files.Items.Cast<TreeViewItem>().FirstOrDefault(curRoomItem => string.Equals(curRoomItem.Header, current.RoomName));

          if (roomItem != null)
          {
            roomItem.Items.Add(new Container { PostedFile = current });
            continue;
          }

          roomItem = new TreeViewItem { Header = current.RoomName };
          roomItem.Items.Add(new Container { PostedFile = current });
          roomItem.ItemTemplate = postedFileTemplate;
          files.Items.Add(roomItem);
        }
      }
    }

    private void RemoveFile_Click(object sender, RoutedEventArgs e)
    {
      PostedFile postedFile = (PostedFile)((Button)sender).Tag;

      if (ClientModel.API != null)
        ClientModel.API.RemoveFileFromRoom(postedFile.RoomName, postedFile.File);

      RefreshFiles();
    }

    private void okBtn_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
    }
  }
}
