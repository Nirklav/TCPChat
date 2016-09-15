using Engine.Api.Client.Rooms;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Net.Sockets;
using System.Windows.Input;
using UI.Infrastructure;
using WPFColor = System.Windows.Media.Color;

namespace UI.ViewModel
{
  public class UserViewModel : BaseViewModel
  {
    #region fields
    private string nick;
    private string nickKey;
    private RoomViewModel parent;
    private bool isClient;
    #endregion

    #region constructors
    public UserViewModel(string userNick, RoomViewModel parentViewModel)
      : this(null, userNick, parentViewModel)
    {

    }

    public UserViewModel(string nickLocKey, string userNick, RoomViewModel parentViewModel)
      : base(parentViewModel, false)
    {
      nick = userNick;
      parent = parentViewModel;
      nickKey = nickLocKey;

      SetRoomAdminCommand = new Command(SetRoomAdmin, _ => ClientModel.Api != null);
      UserClickCommand = new Command(UserClick);

      Localizer.Instance.LocaleChanged += RefreshNick;
    }

    protected override void DisposeManagedResources()
    {
      base.DisposeManagedResources();

      Localizer.Instance.LocaleChanged -= RefreshNick;
    }
    #endregion

    #region commands
    public ICommand SetRoomAdminCommand { get; private set; }
    public ICommand UserClickCommand { get; private set; }
    #endregion

    #region properties
    public WPFColor NickColor
    {
      get
      {
        if (nick == null)
          return WPFColor.FromRgb(0, 0, 0);

        using (var client = ClientModel.Get())
        {
          var user = client.Chat.TryGetUser(nick);
          if (user == null)
            return WPFColor.FromRgb(0, 0, 0);
          return WPFColor.FromRgb(user.NickColor.R, user.NickColor.G, user.NickColor.B);
        }
      }
    }

    public string Nick
    {
      get
      {
        if (nickKey != null)
          return Localizer.Instance.Localize(nickKey);
        return nick;
      }
    }

    private void RefreshNick(object sender, EventArgs args)
    {
      OnPropertyChanged("Nick");
    }

    public bool IsClient
    {
      get { return isClient; }
      set { SetValue(value, "IsClient", v => isClient = v); }
    }

    public bool IsAllInRoom
    {
      get { return nickKey != null; }
    }
    #endregion

    #region command methods
    private void UserClick(object obj)
    {
      parent.Message += Nick + ", ";
      parent.MessageCaretIndex = parent.Message.Length;
    }

    private void SetRoomAdmin(object obj)
    {
      try
      {
        ClientModel.Api.Perform(new ClientSetRoomAdminAction(parent.Name, nick));
      }
      catch (SocketException se)
      {
        parent.AddSystemMessage(se.Message);
      }
    }
    #endregion

    #region methods
    public override bool Equals(object obj)
    {
      if (obj == null)
        return false;

      if (ReferenceEquals(obj, this))
        return true;

      var viewModel = obj as UserViewModel;
      if (viewModel == null)
        return false;

      return Equals(viewModel);
    }

    public bool Equals(UserViewModel viewModel)
    {
      if (viewModel == null)
        return false;

      if (ReferenceEquals(viewModel, this))
        return true;

      if (viewModel.nick == null)
        return nick == null;

      return viewModel.nick == nick;
    }

    public override int GetHashCode()
    {
      if (nick == null)
        return 0;
      return nick.GetHashCode();
    }
    #endregion
  }
}
