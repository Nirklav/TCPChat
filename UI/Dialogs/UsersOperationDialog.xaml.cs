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
using Engine;
using Engine.Concrete;
using Engine.Concrete.Entities;
using UI.ViewModel;

namespace UI.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для UsersOperationDialog.xaml
    /// </summary>
    public partial class UsersOperationDialog : Window
    {
        class UserListItem
        {
            public UserListItem(UserViewModel user)
            {
                User = user;
                Invite = false;
            }

            public UserViewModel User { get; set; }
            public bool Invite { get; set; }
        }

        public IEnumerable<User> Users { get; set; }

        public UsersOperationDialog(string title, IEnumerable<UserViewModel> users)
        {
            InitializeComponent();

            Title = title;

            foreach (UserViewModel user in users)
            {
                UserList.Items.Add(new UserListItem(user));
            }
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            List<User> result = new List<User>();

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
