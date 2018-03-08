using Engine.Api.Client.Rooms;
using Engine.Model.Client;
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
    private string _nick;
    private string _nickKey;
    private RoomViewModel _parent;
    private bool _isClient;
    #endregion

    #region constructors
    public UserViewModel(string userNick, RoomViewModel parentViewModel)
      : this(null, userNick, parentViewModel)
    {

    }

    public UserViewModel(string nickLocKey, string userNick, RoomViewModel parentViewModel)
      : base(parentViewModel, false)
    {
      _nick = userNick;
      _parent = parentViewModel;
      _nickKey = nickLocKey;

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
        if (_nick == null)
          return WPFColor.FromRgb(0, 0, 0);

        using (var client = ClientModel.Get())
        {
          var user = client.Chat.TryGetUser(_nick);
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
        if (_nickKey != null)
          return Localizer.Instance.Localize(_nickKey);
        return _nick;
      }
    }

    private void RefreshNick(object sender, EventArgs args)
    {
      OnPropertyChanged("Nick");
    }

    public bool IsClient
    {
      get { return _isClient; }
      set { SetValue(value, "IsClient", v => _isClient = v); }
    }

    public bool IsAllInRoom
    {
      get { return _nickKey != null; }
    }
    #endregion

    #region command methods
    private void UserClick(object obj)
    {
      _parent.Message += Nick + ", ";
      _parent.MessageCaretIndex = _parent.Message.Length;
    }

    private void SetRoomAdmin(object obj)
    {
      try
      {
        ClientModel.Api.Perform(new ClientSetRoomAdminAction(_parent.Name, _nick));
      }
      catch (SocketException se)
      {
        _parent.AddSystemMessage(se.Message);
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

      if (viewModel._nick == null)
        return _nick == null;

      return viewModel._nick == _nick;
    }

    public override int GetHashCode()
    {
      if (_nick == null)
        return 0;
      return _nick.GetHashCode();
    }
    #endregion
  }
}
