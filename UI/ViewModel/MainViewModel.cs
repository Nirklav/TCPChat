using Engine;
using Engine.Api.Client.Registrations;
using Engine.Api.Client.Rooms;
using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Network;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Input;
using UI.Dialogs;
using UI.Infrastructure;
using UI.View;
using Keys = System.Windows.Forms.Keys;

namespace UI.ViewModel
{
  public class MainViewModel : BaseViewModel
  {
    #region consts
    public const string ProgramName = "TCPChat";

    public const string ParamsErrorKey = "mainViewModel-paramsError";
    public const string APINotSupportedKey = "mainViewModel-apiNotSupported";
    public const string RoomExitQuestionKey = "mainViewModel-roomExitQuestion";
    public const string RoomCloseQuestionKey = "mainViewModel-roomCloseQuestion";
    public const string ServerDisableQuestionKey = "mainViewModel-serverDisableQuestion";
    public const string AudioInitializationFailedKey = "mainViewModel-audioInitializationFailed";

    private const int ClientMaxMessageLength = 100 * 1024;
    private const string PluginsDirectoryName = "plugins";
    #endregion

    #region fields
    private MainWindow _window;
    private int _selectedRoomIndex;
    private RoomViewModel _selectedRoom;

    private volatile bool _keyPressed;
    #endregion

    #region properties
    public bool Alerts
    {
      get { return Settings.Current.Alerts; }
      set
      {
        Settings.Current.Alerts = value;

        OnPropertyChanged(nameof(Alerts));
      }
    }

    public RoomViewModel SelectedRoom
    {
      get { return _selectedRoom; }
      set
      {
        _selectedRoom = value;

        if (_selectedRoom != null && _selectedRoom.Updated)
          _selectedRoom.Updated = false;

        OnPropertyChanged(nameof(SelectedRoom));
      }
    }

    public int SelectedRoomIndex
    {
      get { return _selectedRoomIndex; }
      set
      {
        _selectedRoomIndex = value;
        OnPropertyChanged(nameof(SelectedRoomIndex));
      }
    }

    public ObservableCollection<RoomViewModel> Rooms { get; private set; }
    public ObservableCollection<PluginViewModel> Plugins { get; private set; }
    #endregion

    #region commands
    public ICommand EnableServerCommand { get; private set; }
    public ICommand DisableServerCommand { get; private set; }
    public ICommand ConnectCommand { get; private set; }
    public ICommand DisconnectCommand { get; private set; }
    public ICommand ExitCommand { get; private set; }
    public ICommand CreateRoomCommand { get; private set; }
    public ICommand DeleteRoomCommand { get; private set; }
    public ICommand ExitFromRoomCommand { get; private set; }
    public ICommand OpenFilesDialogCommand { get; private set; }
    public ICommand ShowServerCertificateCommand { get; private set; }
    public ICommand OpenAboutProgramCommand { get; private set; }
    public ICommand OpenSettingsCommand { get; private set; }
    #endregion

    #region constructors
    public MainViewModel(MainWindow mainWindow)
      : base(null, true)
    {
      _window = mainWindow;
      _window.Closed += WindowClosed;
      Rooms = new ObservableCollection<RoomViewModel>();
      Plugins = new ObservableCollection<PluginViewModel>();
      Dispatcher = mainWindow.Dispatcher;

      KeyBoard.KeyDown += OnKeyDown;
      KeyBoard.KeyUp += OnKeyUp;

      Events.Connected += CreateSubscriber<ConnectEventArgs>(ClientConnect);
      Events.ReceiveMessage += CreateSubscriber<ReceiveMessageEventArgs>(ClientReceiveMessage);
      Events.ReceiveRegistrationResponse += CreateSubscriber<RegistrationEventArgs>(ClientRegistration);
      Events.RoomOpened += CreateSubscriber<RoomOpenedEventArgs>(ClientRoomOpened);
      Events.RoomClosed += CreateSubscriber<RoomClosedEventArgs>(ClientRoomClosed);
      Events.AsyncError += CreateSubscriber<AsyncErrorEventArgs>(ClientAsyncError);
      Events.PluginLoaded += CreateSubscriber<PluginEventArgs>(ClientPluginLoaded);
      Events.PluginUnloading += CreateSubscriber<PluginEventArgs>(ClientPluginUnloading);

      EnableServerCommand = new Command(EnableServer, _ => !ServerModel.IsInited && !ClientModel.IsInited);
      DisableServerCommand = new Command(DisableServer, _ => ServerModel.IsInited);
      ConnectCommand = new Command(Connect, _ => !ClientModel.IsInited);
      DisconnectCommand = new Command(Disconnect, _ => ClientModel.IsInited);
      ExitCommand = new Command(_ => _window.Close());
      CreateRoomCommand = new Command(CreateRoom, _ => ClientModel.IsInited);
      DeleteRoomCommand = new Command(DeleteRoom, _ => ClientModel.IsInited);
      ExitFromRoomCommand = new Command(ExitFromRoom, _ => ClientModel.IsInited);
      OpenFilesDialogCommand = new Command(OpenFilesDialog, _ => ClientModel.IsInited);
      ShowServerCertificateCommand = new Command(ShowServerCertificate, _ => ClientModel.IsInited);
      OpenAboutProgramCommand = new Command(OpenAboutProgram);
      OpenSettingsCommand = new Command(OpenSettings);

      ClearTabs();
    }

    protected override void DisposeManagedResources()
    {
      base.DisposeManagedResources();

      KeyBoard.KeyDown -= OnKeyDown;
      KeyBoard.KeyUp -= OnKeyUp;
    }
    #endregion

    #region command methods
    private void EnableServer(object obj)
    {
      var dialog = new ServerDialog();
      if (dialog.ShowDialog() == true)
      {
        try
        {
          var adminPassword = Settings.Current.AdminPassword;
          var p2pPort = Settings.Current.ServerStartP2PPort;
          var excludedPlugins = Settings.Current.Plugins
            .Where(s => !s.Enabled)
            .Select(s => s.Name)
            .ToArray();

          var initializer = new ServerInitializer
          {
            AdminPassword = adminPassword,
            PluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PluginsDirectoryName),
            ExcludedPlugins = excludedPlugins,
            Certificate = new X509Certificate2(dialog.CertificatePath, dialog.CertificatePassword),
          };

          ServerModel.Init(initializer);

          var serverStartUri = Connection.CreateTcpchatUri(dialog.ServerAddress);
          ServerModel.Server.Start(serverStartUri, p2pPort);

          dialog.SaveSettings();
        }
        catch (Exception)
        {
          SelectedRoom.AddSystemMessage(Localizer.Instance.Localize(ParamsErrorKey));

          if (ServerModel.IsInited)
            ServerModel.Reset();
        }
      }
    }

    private void DisableServer(object obj)
    {
      var msg = Localizer.Instance.Localize(ServerDisableQuestionKey);
      if (MessageBox.Show(msg, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
        return;

      if (ClientModel.IsInited)
        ClientModel.Reset();

      if (ServerModel.IsInited)
        ServerModel.Reset();

      ClearTabs();
    }

    private void Connect(object obj)
    {
      var dialog = new ConnectDialog();
      if (dialog.ShowDialog() == true)
      {
        try
        {
          var trustedCertitifcatesPath = Settings.Current.TrustedCertificatesPath;
          var outputAudioDevice = Settings.Current.OutputAudioDevice;
          var inputAudioDevice = Settings.Current.InputAudioDevice;
          var bits = Settings.Current.Bits;
          var frequency = Settings.Current.Frequency;
          var excludedPlugins = Settings.Current.Plugins
            .Where(s => !s.Enabled)
            .Select(s => s.Name)
            .ToArray();

          var initializer = new ClientInitializer
          {
            Nick = dialog.Nick,
            NickColor = dialog.NickColor,
            Certificate = new X509Certificate2(dialog.CertificatePath, dialog.CertificatePassword),
            TrustedCertificatesPath = trustedCertitifcatesPath,

            PluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PluginsDirectoryName),
            ExcludedPlugins = excludedPlugins
          };

          ClientModel.Init(initializer);

          try
          {
            ClientModel.Player.SetOptions(outputAudioDevice);
            ClientModel.Recorder.SetOptions(inputAudioDevice, new AudioQuality(1, bits, frequency));
          }
          catch (ModelException me)
          {
            ClientModel.Player.Dispose();
            ClientModel.Recorder.Dispose();

            if (me.Code != ErrorCode.AudioNotEnabled)
              throw;
            else
            {
              var msg = Localizer.Instance.Localize(AudioInitializationFailedKey);
              MessageBox.Show(msg, ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
          }

          var serverUri = Connection.CreateTcpchatUri(dialog.Address);
          ClientModel.Client.Connect(serverUri);

          dialog.SaveSettings();
        }
        catch (Exception e)
        {
          SelectedRoom.AddSystemMessage(Localizer.Instance.Localize(ParamsErrorKey));

          if (ClientModel.IsInited)
            ClientModel.Reset();
        }
      }
    }

    private void Disconnect(object obj)
    {
      try
      {
        if (ClientModel.Api != null)
          ClientModel.Api.Perform(new ClientUnregisterAction());
      }
      catch (Exception) { }

      if (ClientModel.IsInited)
        ClientModel.Reset();

      ClearTabs();
    }

    private void CreateRoom(object obj)
    {
      try
      {
        var dialog = new CreateRoomDialog();
        if (dialog.ShowDialog() == true && ClientModel.Api != null)
          ClientModel.Api.Perform(new ClientCreateRoomAction(dialog.Name, dialog.Type));
      }
      catch (SocketException se)
      {
        SelectedRoom.AddSystemMessage(se.Message);
      }
    }

    private void DeleteRoom(object obj)
    {
      try
      {
        var msg = Localizer.Instance.Localize(RoomCloseQuestionKey);
        if (MessageBox.Show(msg, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
          return;

        if (ClientModel.Api != null)
          ClientModel.Api.Perform(new ClientDeleteRoomAction(SelectedRoom.Name));
      }
      catch (SocketException se)
      {
        SelectedRoom.AddSystemMessage(se.Message);
      }
    }

    private void ExitFromRoom(object obj)
    {
      try
      {
        var msg = Localizer.Instance.Localize(RoomExitQuestionKey);
        if (MessageBox.Show(msg, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
          return;

        if (ClientModel.Api != null)
          ClientModel.Api.Perform(new ClientExitFromRoomAction(SelectedRoom.Name));
      }
      catch (SocketException se)
      {
        SelectedRoom.AddSystemMessage(se.Message);
      }
    }

    private void OpenFilesDialog(object obj)
    {
      using (var viewModel = new PostedFilesViewModel(this))
      { 
        var dialog = new PostedFilesView();
        dialog.DataContext = viewModel;
        dialog.ShowDialog();
      }
    }

    private void ShowServerCertificate(object obj)
    {
      X509Certificate2UI.DisplayCertificate(ClientModel.Client.RemoteCertiticate);
    }

    private void OpenAboutProgram(object obj)
    {
      var dialog = new AboutProgramDialog();
      dialog.ShowDialog();
    }

    private void OpenSettings(object obj)
    {
      SettingsViewModel viewModel;
      var settings = new SettingsView();
      settings.DataContext = viewModel = new SettingsViewModel(settings);
      settings.ShowDialog();

      viewModel.Dispose();
    }
    #endregion

    #region client events
    private void ClientConnect(ConnectEventArgs args)
    {
      if (args.Error == null)
      {
        switch (ClientModel.Client.RemoteCertificateStatus)
        {
          case CertificateStatus.Trusted:
            ClientModel.Api.Perform(new ClientRegisterAction());
            break;

          case CertificateStatus.SelfSigned:
            var certificate = ClientModel.Client.RemoteCertiticate;
            var dialog = new ServerCertificateConfirmDialog(certificate);
            if (dialog.ShowDialog() == true)
            {
              ClientModel.Api.Perform(new ClientRegisterAction());
            }
            else
            {
              ClientModel.Reset();
            }
            break;

          default:
            ClientModel.Reset();
            break;
        }
      }
      else
      {
        SelectedRoom.AddSystemMessage(args.Error.Message);

        if (ClientModel.IsInited)
          ClientModel.Reset();
      }
    }

    private void ClientRegistration(RegistrationEventArgs e)
    {
      if (!e.Registered)
      {
        SelectedRoom.AddSystemMessage(Localizer.Instance.Localize(e.Message));

        if (ClientModel.IsInited)
          ClientModel.Reset();
      }
    }

    private void ClientReceiveMessage(ReceiveMessageEventArgs e)
    {
      if (e.Type != MessageType.System && e.Type != MessageType.Private)
        return;

      switch (e.Type)
      {
        case MessageType.Private:
          SelectedRoom.AddPrivateMessage(e.Sender, ClientModel.Client.Id, e.Message);
          break;

        case MessageType.System:
          SelectedRoom.AddSystemMessage(Localizer.Instance.Localize(e.SystemMessage, e.SystemMessageFormat));
          break;
      }

      Alert();
    }

    private void ClientRoomOpened(RoomOpenedEventArgs e)
    {
      // If room view model already exist then event will be processed by it self
      if (Rooms.Any(vm => vm.Name == e.RoomName))
        return;

      // Else create new view model
      var roomViewModel = new RoomViewModel(this, e.RoomName);
      roomViewModel.Updated = true;
      Rooms.Add(roomViewModel);

      _window.Alert();
    }

    private void ClientRoomClosed(RoomClosedEventArgs e)
    {
      var roomViewModel = Rooms.FirstOrDefault(vm => vm.Name == e.RoomName);
      if (roomViewModel == null)
        return;

      Rooms.Remove(roomViewModel);
      roomViewModel.Dispose();

      _window.Alert();
    }

    private void ClientAsyncError(AsyncErrorEventArgs e)
    {
      var modelException = e.Error as ModelException;
      if (modelException != null)
      {
        switch (modelException.Code)
        {
          case ErrorCode.ApiNotSupported:
            ClientModel.Reset();
            SelectedRoom.AddSystemMessage(Localizer.Instance.Localize(APINotSupportedKey, modelException.Message));
            return;
        }
      }
    }

    private void ClientPluginLoaded(PluginEventArgs e)
    {
      Plugins.Add(new PluginViewModel(e.PluginName));
    }

    private void ClientPluginUnloading(PluginEventArgs e)
    {
      var pluginViewModel = Plugins.FirstOrDefault(pvm => pvm.PluginName == e.PluginName);
      if (pluginViewModel != null)
      {
        Plugins.Remove(pluginViewModel);
        pluginViewModel.Dispose();
      }
    }

    #endregion

    #region helpers methods
    private void WindowClosed(object sender, EventArgs e)
    {
      if (ClientModel.IsInited)
      {
        try
        {
          if (ClientModel.Api != null)
            ClientModel.Api.Perform(new ClientUnregisterAction());
        }
        catch (Exception)
        {
          // program will be closed and the exception will not affect it.
        }

        ClientModel.Reset();
      }

      if (ServerModel.IsInited)
        ServerModel.Reset();

      Settings.SaveSettings();
    }

    public void Alert()
    {
      _window.Alert();
    }

    private void ClearTabs()
    {
      foreach (var room in Rooms)
        room.Dispose();

      Rooms.Clear();
      Rooms.Add(new RoomViewModel(this));
      SelectedRoomIndex = 0;
    }
    #endregion

    #region record hot key
    private void OnKeyDown(Keys keys)
    {
      var recorderKey = Settings.Current.RecorderKey;

      if ((keys & recorderKey) == recorderKey && !_keyPressed)
      {
        _keyPressed = true;
        if (ClientModel.Recorder != null)
          ClientModel.Recorder.Start();
      }
    }

    private void OnKeyUp(Keys keys)
    {
      var recorderKey = Settings.Current.RecorderKey;

      if ((keys & recorderKey) == recorderKey && _keyPressed)
      {
        _keyPressed = false;
        if (ClientModel.Recorder != null)
          ClientModel.Recorder.Stop();
      }
    }
    #endregion
  }
}