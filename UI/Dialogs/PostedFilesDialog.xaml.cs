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

      using (var client = ClientModel.Get())
      {
        foreach (PostedFile current in client.PostedFiles)
        {
          var roomItem = files.Items
            .Cast<TreeViewItem>()
            .FirstOrDefault(curRoomItem => string.Equals(curRoomItem.Header, current.RoomName));

          if (roomItem == null)
          {
            roomItem = new TreeViewItem { Header = current.RoomName };
            files.Items.Add(roomItem);
          }

          roomItem.Items.Add(new Container { PostedFile = current });
        }
      }
    }

    private void RemoveFile_Click(object sender, RoutedEventArgs e)
    {
      var postedFile = (PostedFile)((Button)sender).Tag;

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
