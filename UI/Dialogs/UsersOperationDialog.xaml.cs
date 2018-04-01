using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using UI.Infrastructure;
using UI.ViewModel;
using Engine.Model.Common.Entities;

namespace UI.Dialogs
{
  public partial class UsersOperationDialog : Window
  {
    private class Item
    {
      public UserId UserId { get; private set; }
      public string Nick { get { return UserId.Nick; } }
      public bool Invite { get; set; }

      public Item(UserId userId)
      {
        UserId = userId;
        Invite = false;
      }
    }

    public IEnumerable<UserId> Users { get; private set; }

    public UsersOperationDialog(string titleKey, IEnumerable<UserId> users)
    {
      InitializeComponent();

      Title = Localizer.Instance.Localize(titleKey);
      foreach (var user in users)
        UserList.Items.Add(new Item(user));
    }

    private void OkBtn_Click(object sender, RoutedEventArgs e)
    {
      Users = UserList.Items
        .Cast<Item>()
        .Where(i => i.Invite)
        .Select(i => i.UserId);

      DialogResult = true;
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }
  }
}
