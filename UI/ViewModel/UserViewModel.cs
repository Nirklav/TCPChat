using Engine;
using Engine.Api.Client.Rooms;
using Engine.Model.Client;
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
    private string _nick;
    private UserCheckStatus _checkStatus;
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
      : base(parentViewModel, true)
    {
      _nick = userNick;
      _parent = parentViewModel;
      _nickKey = nickLocKey;

      if (string.IsNullOrEmpty(_nick))
      {
        _checkStatus = UserCheckStatus.NotChecked;
        _isClient = false;
      }
      else
      {
        using (var client = ClientModel.Get())
        {
          _checkStatus = GetCheckStatus(client);
          _isClient = GetClientStatus(client);
        }
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

    public bool IsClient
    {
      get { return _isClient; }
      set { SetValue(value, nameof(IsClient), v => _isClient = v); }
    }

    public bool IsAllInRoom
    {
      get { return _nickKey != null; }
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

    private void SaveCertificate(object obj)
    {
      using (var client = ClientModel.Get())
      {
        var user = client.Chat.GetUser(_nick);
        ClientModel.TrustedCertificates.Add(user.Certificate);
      }
    }

    private void RemoveCertificate(object obj)
    {
      using (var client = ClientModel.Get())
      {
        var user = client.Chat.GetUser(_nick);
        ClientModel.TrustedCertificates.Remove(user.Certificate);
      }
    }

    private void OpenCertificate(object obj)
    {
      using (var client = ClientModel.Get())
      {
        var user = client.Chat.GetUser(_nick);
        X509Certificate2UI.DisplayCertificate(user.Certificate);
      }
    }
    #endregion

    #region events
    private void RefreshNick(object sender, EventArgs args)
    {
      OnPropertyChanged(nameof(Nick));
    }
    
    private void TrustedCertificatesChanged(TrustedCertificatesEventArgs obj)
    {
      if (!string.IsNullOrEmpty(_nick))
      {
        using (var client = ClientModel.Get())
        {
          var user = client.Chat.GetUser(_nick);
          if (user.Certificate.Equals(obj.Certificate))
            CheckStatus = GetCheckStatus(client);
        }
      }
    }
    #endregion

    #region methods
    private bool GetClientStatus(ClientGuard client)
    {
      return client.Chat.User.Nick == _nick;
    }

    private UserCheckStatus GetCheckStatus(ClientGuard client)
    {
      if (_nick == client.Chat.User.Nick)
        return UserCheckStatus.Checked;

      var user = client.Chat.GetUser(_nick);
      var certificateStatus = Connection.GetCertificateValidationStatus(user.Certificate, ClientModel.TrustedCertificates);

      switch (certificateStatus)
      {
        case CertificateStatus.Trusted:
          var commonName = user.Certificate.GetNameInfo(X509NameType.SimpleName, false);
          var prefix = GenerateCertificateDialog.TcpChatNickPrefix;

          var certificateNick = commonName.StartsWith(prefix)
            ? commonName.Substring(prefix.Length)
            : commonName;

          return certificateNick.Equals(_nick)
            ? UserCheckStatus.Checked
            : UserCheckStatus.CheckedNotMatch;

        case CertificateStatus.SelfSigned:
        case CertificateStatus.Untrusted:
        case CertificateStatus.Unknown:
          return UserCheckStatus.NotChecked;
      }

      return UserCheckStatus.NotChecked;
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
