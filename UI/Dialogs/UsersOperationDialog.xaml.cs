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
using UI.ViewModel;
using Engine.Model.Entities;

namespace UI.Dialogs
{
  public partial class UsersOperationDialog : Window
  {
    private class UserListItem
    {
      public UserListItem(UserViewModel user)
      {
        User = user;
        Invite = false;
      }

      public UserViewModel User { get; private set; }
      public bool Invite { get; set; }
    }

    public IEnumerable<User> Users { get; set; }

    public UsersOperationDialog(string title, IEnumerable<UserViewModel> users)
    {
      InitializeComponent();

      Title = title;

      foreach (var user in users)
        UserList.Items.Add(new UserListItem(user));
    }

    private void OkBtn_Click(object sender, RoutedEventArgs e)
    {
      Users = UserList.Items
        .Cast<UserListItem>()
        .Where(i => i.Invite)
        .Select(i => i.User.Info);

      DialogResult = true;
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }
  }
}
