using Engine;
using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Entities;
using Engine.Model.Server;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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
    #endregion

    #region fields
    private MainWindow window;
    private int selectedRoomIndex;
    private RoomViewModel selectedRoom;

    private volatile bool keyPressed;
    #endregion

    #region properties
    public bool Alerts
    {
      get { return Settings.Current.Alerts; }
      set
      {
        Settings.Current.Alerts = value;

        OnPropertyChanged("Alerts");
      }
    }

    public RoomViewModel SelectedRoom
    {
      get { return selectedRoom; }
      set
      {
        selectedRoom = value;

        if (selectedRoom != null && selectedRoom.Updated)
          selectedRoom.Updated = false;

        OnPropertyChanged("SelectedRoom");
      }
    }

    public int SelectedRoomIndex
    {
      get { return selectedRoomIndex; }
      set
      {
        selectedRoomIndex = value;
        OnPropertyChanged("SelectedRoomIndex");
      }
    }

    public ObservableCollection<RoomViewModel> Rooms { get; private set; }
    public ObservableCollection<UserViewModel> AllUsers { get; private set; }
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
    public ICommand OpenAboutProgramCommand { get; private set; }
    public ICommand OpenSettingsCommand { get; private set; }
    #endregion

    #region constructors
    public MainViewModel(MainWindow mainWindow)
      : base(null, true)
    {
      window = mainWindow;
      window.Closed += WindowClosed;
      Rooms = new ObservableCollection<RoomViewModel>();
      AllUsers = new ObservableCollection<UserViewModel>();
      Plugins = new ObservableCollection<PluginViewModel>();
      Dispatcher = mainWindow.Dispatcher;

      KeyBoard.KeyDown += OnKeyDown;
      KeyBoard.KeyUp += OnKeyUp;

      NotifierContext.Connected += ClientConnect;
      NotifierContext.ReceiveMessage += ClientReceiveMessage;
      NotifierContext.ReceiveRegistrationResponse += ClientRegistration;
      NotifierContext.RoomRefreshed += ClientRoomRefreshed;
      NotifierContext.RoomClosed += ClientRoomClosed;
      NotifierContext.RoomOpened += ClientRoomOpened;
      NotifierContext.AsyncError += ClientAsyncError;
      NotifierContext.PluginLoaded += ClientPluginLoaded;
      NotifierContext.PluginUnloading += ClientPluginUnloading;

      ClearTabs();

      EnableServerCommand = new Command(EnableServer, _ => !ServerModel.IsInited && !ClientModel.IsInited);
      DisableServerCommand = new Command(DisableServer, _ => ServerModel.IsInited);
      ConnectCommand = new Command(Connect, _ => !ClientModel.IsInited);
      DisconnectCommand = new Command(Disconnect, _ => ClientModel.IsInited);
      ExitCommand = new Command(_ => window.Close());
      CreateRoomCommand = new Command(CreateRoom, _ => ClientModel.IsInited);
      DeleteRoomCommand = new Command(DeleteRoom, _ => ClientModel.IsInited);
      ExitFromRoomCommand = new Command(ExitFromRoom, _ => ClientModel.IsInited);
      OpenFilesDialogCommand = new Command(OpenFilesDialog, _ => ClientModel.IsInited);
      OpenAboutProgramCommand = new Command(OpenAboutProgram);
      OpenSettingsCommand = new Command(OpenSettings);
    }

    protected override void DisposeManagedResources()
    {
      base.DisposeManagedResources();

      KeyBoard.KeyDown -= OnKeyDown;
      KeyBoard.KeyUp -= OnKeyUp;

      if (NotifierContext != null)
      {
        NotifierContext.Connected -= ClientConnect;
        NotifierContext.ReceiveMessage -= ClientReceiveMessage;
        NotifierContext.ReceiveRegistrationResponse -= ClientRegistration;
        NotifierContext.RoomRefreshed -= ClientRoomRefreshed;
        NotifierContext.RoomClosed -= ClientRoomClosed;
        NotifierContext.RoomOpened -= ClientRoomOpened;
        NotifierContext.AsyncError -= ClientAsyncError;
        NotifierContext.PluginLoaded -= ClientPluginLoaded;
        NotifierContext.PluginUnloading -= ClientPluginUnloading;
      }
    }
    #endregion

    #region command methods
    private void EnableServer(object obj)
    {
      ServerDialog dialog = new ServerDialog();

      if (dialog.ShowDialog() == true)
      {
        try
        {
          var excludedPlugins = Settings.Current.Plugins
            .Where(s => !s.Enabled)
            .Select(s => s.Name)
            .ToArray();

          var initializer = new ServerInitializer
          {
            PluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"),
            ExcludedPlugins = excludedPlugins
          };

          ServerModel.Init(initializer);
          ServerModel.Server.Start(Settings.Current.Port, Settings.Current.ServicePort, Settings.Current.StateOfIPv6Protocol);

          InitializeClient(true);
        }
        catch (ArgumentException)
        {
          SelectedRoom.AddSystemMessage(Localizer.Instance.Localize(ParamsErrorKey));

          if (ClientModel.IsInited)
            ClientModel.Reset();

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
      ConnectDialog dialog = new ConnectDialog();

      if (dialog.ShowDialog() == true)
        InitializeClient(false);
    }

    private void Disconnect(object obj)
    {
      try
      {
        if (ClientModel.Api != null)
          ClientModel.Api.Unregister();
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
        CreateRoomDialog dialog = new CreateRoomDialog();
        if (dialog.ShowDialog() == true && ClientModel.Api != null)
          ClientModel.Api.CreateRoom(dialog.Name, dialog.Type);
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
          ClientModel.Api.DeleteRoom(SelectedRoom.Name);
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
          ClientModel.Api.ExitFromRoom(SelectedRoom.Name);
      }
      catch (SocketException se)
      {
        SelectedRoom.AddSystemMessage(se.Message);
      }
    }

    private void OpenFilesDialog(object obj)
    {
      PostedFilesDialog dialog = new PostedFilesDialog();
      dialog.ShowDialog();
    }

    private void OpenAboutProgram(object obj)
    {
      AboutProgramDialog dialog = new AboutProgramDialog();
      dialog.ShowDialog();
    }

    private void OpenSettings(object obj)
    {
      SettingsViewModel viewModel;
      SettingsView settings = new SettingsView();
      settings.DataContext = viewModel = new SettingsViewModel(settings);
      settings.ShowDialog();

      viewModel.Dispose();
    }
    #endregion

    #region client events
    private void ClientConnect(object sender, ConnectEventArgs e)
    {
      Dispatcher.BeginInvoke(new Action<ConnectEventArgs>(args =>
      {
        if (args.Error != null)
        {
          SelectedRoom.AddSystemMessage(args.Error.Message);

          if (ClientModel.IsInited)
            ClientModel.Reset();

          return;
        }

        if (ClientModel.Api != null)
          ClientModel.Api.Register();
      }), e);
    }

    private void ClientRegistration(object sender, RegistrationEventArgs e)
    {
      Dispatcher.BeginInvoke(new Action<RegistrationEventArgs>(args =>
      {
        if (!args.Registered)
        {
          SelectedRoom.AddSystemMessage(Localizer.Instance.Localize(args.Message));

          if (ClientModel.IsInited)
            ClientModel.Reset();
        }
      }), e);
    }

    private void ClientReceiveMessage(object sender, ReceiveMessageEventArgs e)
    {
      if (e.Type != MessageType.System && e.Type != MessageType.Private)
        return;

      Dispatcher.BeginInvoke(new Action<ReceiveMessageEventArgs>(args =>
      {
        switch (args.Type)
        {
          case MessageType.Private:
            using (var client = ClientModel.Get())
            {
              UserViewModel senderUser = AllUsers.Single(uvm => string.Equals(uvm.Info.Nick, args.Sender));
              UserViewModel receiverUser = AllUsers.Single(uvm => string.Equals(uvm.Info.Nick, client.User.Nick));
              SelectedRoom.AddPrivateMessage(senderUser, receiverUser, args.Message);
            }
            break;

          case MessageType.System:
            SelectedRoom.AddSystemMessage(Localizer.Instance.Localize(args.SystemMessage, args.SystemMessageFormat));
            break;
        }

        Alert();
      }), e);
    }

    private void ClientRoomRefreshed(object sender, RoomEventArgs e)
    {
      Dispatcher.BeginInvoke(new Action<RoomEventArgs>(args =>
      {
        if (args.Room.Name == ServerModel.MainRoomName)
        {
          using (var client = ClientModel.Get())
          {
            // Remove items
            for (int i = AllUsers.Count - 1; i >= 0; i--)
            {
              var uvm = AllUsers[i];
              if (args.Users.Exists(u => u.Nick == uvm.Nick))
                continue;
              AllUsers.RemoveAt(i);
            }

            // Add unexisting items
            foreach (var user in args.Users)
            {
              if (AllUsers.Any(uvm => uvm.Nick == user.Nick))
                continue;

              var userViewModel = new UserViewModel(user, null);
              userViewModel.IsClient = user.Equals(client.User);
              AllUsers.Add(userViewModel);
            }
          }
        }
      }), e);
    }

    private void ClientRoomOpened(object sender, RoomEventArgs e)
    {
      Dispatcher.BeginInvoke(new Action<RoomEventArgs>(args =>
      {
        if (Rooms.FirstOrDefault(roomVM => roomVM.Name == args.Room.Name) != null)
          return;

        RoomViewModel roomViewModel = new RoomViewModel(this, args.Room, args.Users);
        roomViewModel.Updated = true;
        Rooms.Add(roomViewModel);

        window.Alert();
      }), e);
    }

    private void ClientRoomClosed(object sender, RoomEventArgs e)
    {
      Dispatcher.BeginInvoke(new Action<RoomEventArgs>(args =>
      {
        RoomViewModel roomViewModel = Rooms.FirstOrDefault(roomVM => roomVM.Name == args.Room.Name);

        if (roomViewModel == null)
          return;

        Rooms.Remove(roomViewModel);
        roomViewModel.Dispose();

        window.Alert();
      }), e);
    }

    private void ClientAsyncError(object sender, AsyncErrorEventArgs e)
    {
      Dispatcher.BeginInvoke(new Action<AsyncErrorEventArgs>(args =>
      {
        ModelException modelException = args.Error as ModelException;

        if (modelException != null)
          switch (modelException.Code)
          {
            case ErrorCode.APINotSupported:
              ClientModel.Reset();
              SelectedRoom.AddSystemMessage(Localizer.Instance.Localize(APINotSupportedKey, modelException.Message));
              return;
          }
      }), e);
    }

    private void ClientPluginLoaded(object sender, PluginEventArgs e)
    {
      Dispatcher.BeginInvoke(new Action<PluginEventArgs>(args =>
      {
        Plugins.Add(new PluginViewModel(args.PluginName));
      }), e);
    }

    private void ClientPluginUnloading(object sender, PluginEventArgs e)
    {
      Dispatcher.BeginInvoke(new Action<PluginEventArgs>(args =>
      {
        var pluginViewModel = Plugins.FirstOrDefault(pvm => pvm.PluginName == e.PluginName);
        if (pluginViewModel != null)
        {
          Plugins.Remove(pluginViewModel);
          pluginViewModel.Dispose();
        }
      }), e);
    }

    #endregion

    #region helpers methods
    private void InitializeClient(bool loopback)
    {
      var excludedPlugins = Settings.Current.Plugins
        .Where(s => !s.Enabled)
        .Select(s => s.Name)
        .ToArray();

      var initializer = new ClientInitializer
      {
        Nick = Settings.Current.Nick,
        NickColor = Settings.Current.NickColor,

        PluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"),
        ExcludedPlugins = excludedPlugins
      };

      ClientModel.Init(initializer);

      try
      {
        ClientModel.Player.SetOptions(Settings.Current.OutputAudioDevice);
        ClientModel.Recorder.SetOptions(Settings.Current.InputAudioDevice, new AudioQuality(1, Settings.Current.Bits, Settings.Current.Frequency));
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

      IPAddress address = loopback
        ? Settings.Current.StateOfIPv6Protocol ? IPAddress.IPv6Loopback : IPAddress.Loopback
        : IPAddress.Parse(Settings.Current.Address);

      ClientModel.Client.Connect(new IPEndPoint(address, Settings.Current.Port));
    }

    private void WindowClosed(object sender, EventArgs e)
    {
      if (ClientModel.IsInited)
      {
        try
        {
          if (ClientModel.Api != null)
            ClientModel.Api.Unregister();
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
      window.Alert();
    }

    private void ClearTabs()
    {
      AllUsers.Clear();

      foreach (var room in Rooms)
        room.Dispose();

      Rooms.Clear();
      Rooms.Add(new RoomViewModel(this, new Room(null, ServerModel.MainRoomName), null));
      SelectedRoomIndex = 0;
    }
    #endregion

    #region record hot key
    private void OnKeyDown(Keys keys)
    {
      var recorderKey = Settings.Current.RecorderKey;

      if ((keys & recorderKey) == recorderKey && !keyPressed)
      {
        keyPressed = true;
        if (ClientModel.Recorder != null)
          ClientModel.Recorder.Start();
      }
    }

    private void OnKeyUp(Keys keys)
    {
      var recorderKey = Settings.Current.RecorderKey;

      if ((keys & recorderKey) == recorderKey && keyPressed)
      {
        keyPressed = false;
        if (ClientModel.Recorder != null)
          ClientModel.Recorder.Stop();
      }
    }
    #endregion
  }
}