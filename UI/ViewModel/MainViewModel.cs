using Engine;
using Engine.API.StandardAPI;
using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Entities;
using Engine.Model.Server;
using OpenAL;
using System;
using System.Collections.ObjectModel;
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
    private const string ProgramName = "TCPChat";
    private const string ParamsError = "Ошибка входных данных.";
    private const string ClientNotCreated = "Клинет не соединен ни с каким сервером. Установите соединение.";
    private const string APINotSupported = "Приложение не поддерживает эту версию API ({0}). Соединение разорвано.";
    private const string RegFailNickAlreadyExist = "Ник уже занят. Вы не зарегистрированны.";
    private const string RoomExitQuestion = "Вы действительно хотите выйти из комнаты?";
    private const string RoomCloseQuestion = "Вы точно хотите закрыть комнату?";
    private const string ServerDisableQuestion = "Вы точно хотите выключить сервер?";
    private const string FileMustDontExist = "Необходимо выбрать несуществующий файл.";
    private const string AllInRoom = "Все в комнате";

    private const int ClientMaxMessageLength = 100 * 1024;
    private const Keys RecorderKey = Keys.E;
    #endregion

    #region fields
    private MainWindow window;
    private int selectedRoomIndex;
    private RoomViewModel selectedRoom;
    private volatile bool keyPressed;
    #endregion

    #region properties
    public Dispatcher Dispatcher { get; private set; }

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

        if (selectedRoom.Updated)
          selectedRoom.Updated = false;

        OnPropertyChanged("SelectedRoom");
      }
    }

    public int SelectedRoomIndex
    {
      get { return selectedRoomIndex; }
      set
      {
        if (value < 0)
        {
          selectedRoomIndex = 0;
          Dispatcher.BeginInvoke(new Action(() => OnPropertyChanged("SelectedRoomIndex")), DispatcherPriority.Render);
          return;
        }

        selectedRoomIndex = value;

        OnPropertyChanged("SelectedRoomIndex");
      }
    }

    public ObservableCollection<RoomViewModel> Rooms { get; private set; }
    public ObservableCollection<UserViewModel> AllUsers { get; private set; }
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
    {
      window = mainWindow;
      window.Closed += WindowClosed;
      Rooms = new ObservableCollection<RoomViewModel>();
      AllUsers = new ObservableCollection<UserViewModel>();
      Dispatcher = mainWindow.Dispatcher;

      mainWindow.KeyDown += OnKeyDown;
      mainWindow.KeyUp += OnKeyUp;

      ClientModel.Connected += ClientConnect;
      ClientModel.ReceiveMessage += ClientReceiveMessage;
      ClientModel.ReceiveRegistrationResponse += ClientRegistration;
      ClientModel.RoomRefreshed += ClientRoomRefreshed;
      ClientModel.RoomClosed += ClientRoomClosed;
      ClientModel.RoomOpened += ClientRoomOpened;
      ClientModel.AsyncError += ClientAsyncError;

      ClearTabs();

      EnableServerCommand = new Command(EnableServer, obj => ServerModel.Server == null);
      DisableServerCommand = new Command(DisableServer, obj => ServerModel.Server != null);
      ConnectCommand = new Command(Connect, obj => ClientModel.Client == null);
      DisconnectCommand = new Command(Disconnect, obj => ClientModel.Client != null);
      ExitCommand = new Command(obj => window.Close());
      CreateRoomCommand = new Command(CreateRoom, obj => ClientModel.Client != null);
      DeleteRoomCommand = new Command(DeleteRoom, obj => ClientModel.Client != null);
      ExitFromRoomCommand = new Command(ExitFromRoom, obj => ClientModel.Client != null);
      OpenFilesDialogCommand = new Command(OpenFilesDialog, obj => ClientModel.Client != null);
      OpenAboutProgramCommand = new Command(OpenAboutProgram);
      OpenSettingsCommand = new Command(OpenSettings);
    }

    public override void Dispose()
    {
      base.Dispose();

      ClientModel.Connected -= ClientConnect;
      ClientModel.ReceiveMessage -= ClientReceiveMessage;
      ClientModel.ReceiveRegistrationResponse -= ClientRegistration;
      ClientModel.RoomRefreshed -= ClientRoomRefreshed;
      ClientModel.RoomClosed -= ClientRoomClosed;
      ClientModel.RoomOpened -= ClientRoomOpened;
      ClientModel.AsyncError -= ClientAsyncError;
    }
    #endregion

    #region command methods
    public void EnableServer(object obj)
    {
      ServerDialog dialog = new ServerDialog(
        Settings.Current.Nick, 
        Settings.Current.NickColor, 
        Settings.Current.Port, 
        Settings.Current.StateOfIPv6Protocol);

      if (dialog.ShowDialog() == true)
      {
        try
        {
          Settings.Current.Nick = dialog.Nick;
          Settings.Current.NickColor = dialog.NickColor;
          Settings.Current.Port = dialog.Port;
          Settings.Current.StateOfIPv6Protocol = dialog.UsingIPv6Protocol;

          ServerModel.Init(new StandardServerAPI());
          ServerModel.Server.Start(dialog.Port, dialog.UsingIPv6Protocol);

          IPAddress address = dialog.UsingIPv6Protocol ? IPAddress.IPv6Loopback : IPAddress.Loopback;

          ClientModel.Init(dialog.Nick, dialog.NickColor);

          string outputDevice = string.IsNullOrEmpty(Settings.Current.InputAudioDevice)
            ? AudioContext.DefaultDevice
            : Settings.Current.OutputAudioDevice;

          string inputDevice = string.IsNullOrEmpty(Settings.Current.InputAudioDevice)
            ? AudioCapture.DefaultDevice
            : Settings.Current.InputAudioDevice;

          ClientModel.Player.SetOptions(outputDevice);
          ClientModel.Recorder.SetOptions(inputDevice, new AudioQuality(1, Settings.Current.Bits, Settings.Current.Frequency));
          ClientModel.Client.Connect(new IPEndPoint(address, dialog.Port));
        }
        catch (ArgumentException)
        {
          SelectedRoom.AddSystemMessage(ParamsError);

          if (ClientModel.IsInited)
            ClientModel.Reset();

          if (ServerModel.IsInited)
            ServerModel.Reset();
        }
      }
    }

    public void DisableServer(object obj)
    {
      if (MessageBox.Show(ServerDisableQuestion, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
        return;

      if (ClientModel.IsInited)
        ClientModel.Reset();

      if (ServerModel.IsInited)
        ServerModel.Reset();

      ClearTabs();
    }

    public void Connect(object obj)
    {
      ConnectDialog dialog = new ConnectDialog(
        Settings.Current.Nick, 
        Settings.Current.NickColor, 
        Settings.Current.Address, 
        Settings.Current.Port);

      if (dialog.ShowDialog() == true)
      {
        Settings.Current.Nick = dialog.Nick;
        Settings.Current.NickColor = dialog.NickColor;
        Settings.Current.Port = dialog.Port;
        Settings.Current.Address = dialog.Address.ToString();

        ClientModel.Init(dialog.Nick, dialog.NickColor);

        string outputDevice = string.IsNullOrEmpty(Settings.Current.InputAudioDevice)
          ? AudioContext.DefaultDevice
          : Settings.Current.OutputAudioDevice;

        string inputDevice = string.IsNullOrEmpty(Settings.Current.InputAudioDevice) 
          ? AudioCapture.DefaultDevice 
          : Settings.Current.InputAudioDevice;

        ClientModel.Player.SetOptions(outputDevice);
        ClientModel.Recorder.SetOptions(inputDevice, new AudioQuality(1, Settings.Current.Bits, Settings.Current.Frequency));
        ClientModel.Client.Connect(new IPEndPoint(dialog.Address, dialog.Port));
      }
    }

    public void Disconnect(object obj)
    {
      try
      {
        if (ClientModel.API != null)
          ClientModel.API.Unregister();
      }
      catch (Exception) { }

      if (ClientModel.IsInited)
        ClientModel.Reset();

      ClearTabs();
    }

    public void CreateRoom(object obj)
    {
      try
      {
        CreateRoomDialog dialog = new CreateRoomDialog();
        if (dialog.ShowDialog() == true && ClientModel.API != null)
          ClientModel.API.CreateRoom(dialog.RoomName, RoomType.Voice);
      }
      catch (SocketException se)
      {
        SelectedRoom.AddSystemMessage(se.Message);
      }
    }

    public void DeleteRoom(object obj)
    {
      try
      {
        if (MessageBox.Show(RoomCloseQuestion, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
          return;

        if (ClientModel.API != null)
          ClientModel.API.DeleteRoom(SelectedRoom.Name);
      }
      catch (SocketException se)
      {
        SelectedRoom.AddSystemMessage(se.Message);
      }
    }

    public void ExitFromRoom(object obj)
    {
      try
      {
        if (MessageBox.Show(RoomExitQuestion, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
          return;

        if (ClientModel.API != null)
          ClientModel.API.ExitFormRoom(SelectedRoom.Name);
      }
      catch (SocketException se)
      {
        SelectedRoom.AddSystemMessage(se.Message);
      }
    }

    public void OpenFilesDialog(object obj)
    {
      PostedFilesDialog dialog = new PostedFilesDialog();
      dialog.ShowDialog();
    }

    public void OpenAboutProgram(object obj)
    {
      AboutProgramDialog dialog = new AboutProgramDialog();
      dialog.ShowDialog();
    }

    private void OpenSettings(object obj)
    {
      SettingsView settings = new SettingsView();
      settings.DataContext = new SettingsViewModel(settings);
      settings.ShowDialog();
    }
    #endregion

    #region client events
    private void ClientConnect(object sender, ConnectEventArgs e)
    {
      Dispatcher.Invoke(new Action<ConnectEventArgs>(args =>
      {
        if (args.Error != null)
        {
          SelectedRoom.AddSystemMessage(args.Error.Message);

          if (ClientModel.IsInited)
            ClientModel.Reset();

          return;
        }

        if (ClientModel.API != null)
          ClientModel.API.Register();
      }), e);
    }

    private void ClientRegistration(object sender, RegistrationEventArgs e)
    {
      Dispatcher.Invoke(new Action<RegistrationEventArgs>(args =>
      {
        if (!args.Registered)
        {
          SelectedRoom.AddSystemMessage(RegFailNickAlreadyExist);

          if (ClientModel.IsInited)
            ClientModel.Reset();
        }
      }), e);
    }

    private void ClientReceiveMessage(object sender, ReceiveMessageEventArgs e)
    {
      if (e.Type != MessageType.System && e.Type != MessageType.Private)
        return;

      Dispatcher.Invoke(new Action<ReceiveMessageEventArgs>(args =>
      {
        using (var client = ClientModel.Get())
          switch (args.Type)
          {
            case MessageType.Private:
              UserViewModel senderUser = AllUsers.Single(uvm => uvm.Info.Nick == args.Sender);
              UserViewModel receiverUser = AllUsers.Single(uvm => uvm.Info.Equals(client.User));
              SelectedRoom.AddPrivateMessage(senderUser, receiverUser, args.Message);
              break;

            case MessageType.System:
              SelectedRoom.AddSystemMessage(args.Message);
              break;
          }

        Alert();
      }), e);
    }

    private void ClientRoomRefreshed(object sender, RoomEventArgs e)
    {
      Dispatcher.Invoke(new Action<RoomEventArgs>(args =>
      {
        if (args.Room.Name == ServerModel.MainRoomName)
        {
          AllUsers.Clear();

          using(var client = ClientModel.Get())
            foreach (string nick in args.Room.Users)
            {
              User user = args.Users.Single(u => u.Equals(nick));

              if (user.Equals(client.User))
                AllUsers.Add(new UserViewModel(user, null) { IsClient = true });
              else
                AllUsers.Add(new UserViewModel(user, null));
            }
        }
      }), e);
    }

    private void ClientRoomOpened(object sender, RoomEventArgs e)
    {
      Dispatcher.Invoke(new Action<RoomEventArgs>(args =>
      {
        if (Rooms.FirstOrDefault(roomVM => roomVM.Name == args.Room.Name) != null)      
          return;
        
        RoomViewModel roomViewModel = new RoomViewModel(this, args.Room, e.Users);
        roomViewModel.Updated = true;
        Rooms.Add(roomViewModel);

        window.Alert();
      }), e);
    }

    private void ClientRoomClosed(object sender, RoomEventArgs e)
    {
      Dispatcher.Invoke(new Action<RoomEventArgs>(args =>
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
      Dispatcher.Invoke(new Action<AsyncErrorEventArgs>(args =>
      {
        ModelException modelException = e.Error as ModelException;

        if (modelException != null)
          switch (modelException.Code)
          {
            case ErrorCode.APINotSupported:
              ClientModel.Reset();
              SelectedRoom.AddSystemMessage(string.Format(APINotSupported, modelException.State));
              return;
          }

        SelectedRoom.AddSystemMessage(args.Error.Message);
      }), e);
    }
    #endregion

    #region helpers methods
    private void WindowClosed(object sender, EventArgs e)
    {
      if (ClientModel.IsInited)
      {
        try
        {
          if (ClientModel.API != null)
            ClientModel.API.Unregister();
        }
        catch (SocketException) { }

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

      foreach (RoomViewModel room in Rooms)
        room.Dispose();

      Rooms.Clear();
      Rooms.Add(new RoomViewModel(this, new Room(null, ServerModel.MainRoomName), null));
      SelectedRoomIndex = 0;
    }
    #endregion

    #region record hot key
    private void OnKeyUp(Keys keys)
    {
      if ((keys & RecorderKey) == RecorderKey && keyPressed)
      {
        keyPressed = false;
        if (ClientModel.Recorder != null)
          ClientModel.Recorder.Stop();
      }
    }

    private void OnKeyDown(Keys keys)
    {
      if ((keys & RecorderKey) == RecorderKey && !keyPressed)
      {
        keyPressed = true;
        if (ClientModel.Recorder != null)
          ClientModel.Recorder.Start();
      }
    }
    #endregion
  }
}