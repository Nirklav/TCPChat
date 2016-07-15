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
using UI.Infrastructure;

namespace UI.Dialogs
{
  public partial class UsersOperationDialog : Window
  {
    private class UserListItem
    {
      public UserListItem(string nick)
      {
        Nick = nick;
        Invite = false;
      }

      public string Nick { get; private set; }
      public bool Invite { get; set; }
    }

    public IEnumerable<string> Users { get; set; }

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
