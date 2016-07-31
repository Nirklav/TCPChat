using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using UI.Infrastructure;
using UI.ViewModel;

namespace UI.Dialogs
{
  public partial class UsersOperationDialog : Window
  {
    private class UserListItem
    {
      public string Nick { get; private set; }
      public bool Invite { get; set; }

      public UserListItem(string nick)
      {
        Nick = nick;
        Invite = false;
      }
    }

    public IEnumerable<string> Users { get; private set; }

    public UsersOperationDialog(string titleKey, IEnumerable<string> users)
    {
      InitializeComponent();

      Title = Localizer.Instance.Localize(titleKey);
      foreach (var user in users)
        UserList.Items.Add(new UserListItem(user));
    }

    private void OkBtn_Click(object sender, RoutedEventArgs e)
    {
      Users = UserList.Items
        .Cast<UserListItem>()
        .Where(i => i.Invite)
        .Select(i => i.Nick);

      DialogResult = true;
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }
  }
}
