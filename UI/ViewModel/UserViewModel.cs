using Engine.Model.Client;
using Engine.Model.Entities;
using System.Net.Sockets;
using System.Windows.Input;
using UI.Infrastructure;
using WPFColor = System.Windows.Media.Color;

namespace UI.ViewModel
{
  public class UserViewModel : BaseViewModel
  {
    #region fields
    private bool isClient;
    #endregion

    #region properties
    public User Info { get; private set; }
    public RoomViewModel RoomViewModel { get; private set; }
    #endregion

    #region constructors
    public UserViewModel(User info, RoomViewModel roomViewModel)
      : base(false)
    {
      Info = info;
      RoomViewModel = roomViewModel;

      SetRoomAdminCommand = new Command(SetRoomAdmin, Obj => ClientModel.Client != null);
      UserClickCommand = new Command(UserClick);
    }
    #endregion

    #region commands
    public ICommand SetRoomAdminCommand { get; private set; }
    public ICommand UserClickCommand { get; private set; }
    #endregion

    #region properties
    public WPFColor NickColor
    {
      get { return WPFColor.FromRgb(Info.NickColor.R, Info.NickColor.G, Info.NickColor.B); }
    }

    public string Nick
    {
      get { return Info.Nick; }
    }

    public bool IsClient
    {
      get { return isClient; }
      set { SetValue(value, "IsClient", v => isClient = v); }
    }
    #endregion

    #region command methods
    private void UserClick(object obj)
    {
      RoomViewModel.Message += Nick + ", ";
      RoomViewModel.MessageCaretIndex = RoomViewModel.Message.Length;
    }

    private void SetRoomAdmin(object obj)
    {
      try
      {
        if (ClientModel.Api != null)
          ClientModel.Api.SetRoomAdmin(RoomViewModel.MainViewModel.SelectedRoom.Name, Info);
      }
      catch (SocketException se)
      {
        RoomViewModel.AddSystemMessage(se.Message);
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

      UserViewModel viewModel = obj as UserViewModel;

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

      if (viewModel.Info == null)
        return Info == null;

      return viewModel.Info.Equals(Info);
    }

    public override int GetHashCode()
    {
      if (Info == null)
        return 0;

      return Info.GetHashCode();
    }
    #endregion
  }
}
