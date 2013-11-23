using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TCPChat.Engine;
using TCPChat.Engine.Connections;

namespace TCPChat.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для FilesWindow.xaml
    /// </summary>
    public partial class PostedFilesDialog : Window
    {
        ClientConnection client;

        class Container
        {
            public PostedFile PostedFile { get; set; }
        }

        public PostedFilesDialog(ClientConnection clientConnection)
        {
            InitializeComponent();

            client = clientConnection;
            RefreshFiles();
        }

        private void RefreshFiles()
        {
            files.Items.Clear();

            DataTemplate postedFileTemplate = (DataTemplate)FindResource("PostedFileTemplate");

            foreach (PostedFile current in client.PostedFiles)
            {
                TreeViewItem roomItem = files.Items.Cast<TreeViewItem>().FirstOrDefault((curRoomItem) => string.Equals(curRoomItem.Header, current.RoomName));

                if (roomItem != null)
                {
                    roomItem.Items.Add(new Container() { PostedFile = current });
                    continue;
                }

                roomItem = new TreeViewItem() { Header = current.RoomName };
                roomItem.Items.Add(new Container() { PostedFile = current });
                roomItem.ItemTemplate = postedFileTemplate;
                files.Items.Add(roomItem);
            }
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            PostedFile postedFile = (PostedFile)((Button)sender).Tag;
            client.RemoveFileFromRoomAsyc(postedFile.RoomName, postedFile.File);

            RefreshFiles();
        }

        private void okBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
