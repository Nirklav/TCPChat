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

namespace TCPChat.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для UsersOperationDialog.xaml
    /// </summary>
    public partial class UsersOperationDialog : Window
    {
        class UserListItem
        {
            public UserListItem(UserContainer user)
            {
                User = user;
                Invite = false;
            }

            public UserContainer User { get; set; }
            public bool Invite { get; set; }
        }

        public IEnumerable<UserDescription> Users { get; set; }

        public UsersOperationDialog(string title, IEnumerable<UserContainer> users)
        {
            InitializeComponent();

            Title = title;

            foreach (UserContainer user in users)
            {
                UserList.Items.Add(new UserListItem(user));
            }
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            List<UserDescription> result = new List<UserDescription>();

            foreach (UserListItem item in UserList.Items.Cast<UserListItem>())
                if (item.Invite)
                    result.Add(item.User.Info);

            Users = result;

            DialogResult = true;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
