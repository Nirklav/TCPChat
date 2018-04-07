using Engine;
using Engine.Api.Client.Rooms;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using Engine.Network;
using System;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Input;
using UI.Dialogs;
using UI.Infrastructure;
using WPFColor = System.Windows.Media.Color;

namespace UI.ViewModel
{
  public class UserViewModel : BaseViewModel
  {
    #region fields
    private UserId _userId;
    private WPFColor _nickColor;
    private UserCheckStatus _checkStatus;
    private RoomViewModel _parent;
    private bool _isClient;
    #endregion

    #region constructors
    public UserViewModel(UserId userId, RoomViewModel parentViewModel)
      : this(null, userId, parentViewModel)
    {

    }

    public UserViewModel(string nickLKey, UserId userId, RoomViewModel parentViewModel)
      : base(parentViewModel, true)
    {
      _userId = userId;
      _parent = parentViewModel;

      using (var client = ClientModel.Get())
      {
        _checkStatus = GetCheckStatus(client);
        _isClient = GetClientStatus(client);
        _nickColor = GetColor(client);
      }

      UserClickCommand = new Command(UserClick);
      SetRoomAdminCommand = new Command(SetRoomAdmin, _ => ClientModel.Api != null);
      OpenCertificateCommand = new Command(OpenCertificate, _ => ClientModel.Api != null);
      SaveCertificateCommand = new Command(SaveCertificate, _ => ClientModel.Api != null);
      RemoveCertificateCommand = new Command(RemoveCertificate, _ => ClientModel.Api != null);

      Events.TrustedCertificatesChanged += CreateSubscriber<TrustedCertificatesEventArgs>(TrustedCertificatesChanged);

      Localizer.Instance.LocaleChanged += RefreshNick;
    }

    protected override void DisposeManagedResources()
    {
      base.DisposeManagedResources();

      Localizer.Instance.LocaleChanged -= RefreshNick;
    }
    #endregion

    #region commands
    public ICommand UserClickCommand { get; private set; }
    public ICommand SetRoomAdminCommand { get; private set; }
    public ICommand OpenCertificateCommand { get; private set; }
    public ICommand SaveCertificateCommand { get; private set; }
    public ICommand RemoveCertificateCommand { get; private set; }
    #endregion

    #region properties
    public UserId UserId
    {
      get { return _userId; }
    }

    public string Nick
    {
      get { return _userId.Nick; }
    }

    public WPFColor NickColor
    {
      get { return _nickColor; }
    }

    public bool IsClient
    {
      get { return _isClient; }
      set { SetValue(value, nameof(IsClient), v => _isClient = v); }
    }

    public UserCheckStatus CheckStatus
    {
      get { return _checkStatus; }
      set { SetValue(value, nameof(CheckStatus), v => _checkStatus = v); }
    }
    #endregion

    #region command methods
    private void UserClick(object obj)
    {
      _parent.Message += UserId.Nick + ", ";
      _parent.MessageCaretIndex = _parent.Message.Length;
    }

    private void SetRoomAdmin(object obj)
    {
      try
      {
        ClientModel.Api.Perform(new ClientSetRoomAdminAction(_parent.Name, _userId));
      }
      catch (SocketException se)
      {
        _parent.AddSystemMessage(se.Message);
      }
    }

    private void SaveCertificate(object obj)
    {
      using (var client = ClientModel.Get())
      {
        var user = client.Chat.GetUser(_userId);
        ClientModel.TrustedCertificates.Add(user.Certificate);
      }
    }

    private void RemoveCertificate(object obj)
    {
      using (var client = ClientModel.Get())
      {
        var user = client.Chat.GetUser(_userId);
        ClientModel.TrustedCertificates.Remove(user.Certificate);
      }
    }

    private void OpenCertificate(object obj)
    {
      using (var client = ClientModel.Get())
      {
        var user = client.Chat.GetUser(_userId);
        X509Certificate2UI.DisplayCertificate(user.Certificate);
      }
    }
    #endregion

    #region events
    private void RefreshNick(object sender, EventArgs args)
    {
      OnPropertyChanged(nameof(UserId));
    }
    
    private void TrustedCertificatesChanged(TrustedCertificatesEventArgs obj)
    {
      if (_userId != UserId.Empty)
      {
        using (var client = ClientModel.Get())
        {
          var user = client.Chat.GetUser(_userId);
          if (user.Certificate.Equals(obj.Certificate))
            CheckStatus = GetCheckStatus(client);
        }
      }
    }
    #endregion

    #region methods
    private bool GetClientStatus(ClientGuard client)
    {
      return client.Chat.User.Id == _userId;
    }

    private UserCheckStatus GetCheckStatus(ClientGuard client)
    {
      if (_userId == client.Chat.User.Id)
        return UserCheckStatus.Checked;

      var user = client.Chat.GetUser(_userId);
      var certificateStatus = Connection.GetCertificateValidationStatus(user.Certificate, ClientModel.TrustedCertificates);

      switch (certificateStatus)
      {
        case CertificateStatus.Trusted:
          var commonName = user.Certificate.GetNameInfo(X509NameType.SimpleName, false);
          var prefix = GenerateCertificateDialog.TcpChatNickPrefix;

          var certificateNick = commonName.StartsWith(prefix)
            ? commonName.Substring(prefix.Length)
            : commonName;

          return certificateNick.Equals(_userId.Nick)
            ? UserCheckStatus.Checked
            : UserCheckStatus.CheckedNotMatch;

        case CertificateStatus.SelfSigned:
        case CertificateStatus.Untrusted:
        case CertificateStatus.Unknown:
          return UserCheckStatus.NotChecked;
      }

      return UserCheckStatus.NotChecked;
    }
    
    private WPFColor GetColor(ClientGuard client)
    {
      var user = client.Chat.GetUser(_userId);
      return WPFColor.FromRgb(user.NickColor.R, user.NickColor.G, user.NickColor.B);
    }

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

      if (viewModel._userId == UserId.Empty)
        return _userId == UserId.Empty;

      return viewModel._userId == _userId;
    }

    public override int GetHashCode()
    {
      return _userId.GetHashCode();
    }
    #endregion
  }
}
