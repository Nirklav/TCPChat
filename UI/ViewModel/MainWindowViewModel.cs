using Engine.Concrete;
using Engine.Concrete.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using UI.Dialogs;
using UI.Infrastructure;
using UI.View;

namespace UI.ViewModel
{
  public class MainWindowViewModel : BaseViewModel
  {
    #region consts
    private const string ProgramName = "TCPChat";
    private const string ParamsError = "Ошибка входных данных.";
    private const string ClientNotCreated = "Клинет не соединен ни с каким сервером. Установите соединение.";
    private const string RegFailNickAlreadyExist = "Ник уже занят. Вы не зарегистрированны.";
    private const string RoomExitQuestion = "Вы действительно хотите выйти из комнаты?";
    private const string RoomCloseQuestion = "Вы точно хотите закрыть комнату?";
    private const string ServerDisableQuestion = "Вы точно хотите выключить сервер?";
    private const string FileMustDontExist = "Необходимо выбрать несуществующий файл.";
    private const string AllInRoom = "Все в комнате";

    private const int ClientMaxMessageLength = 100 * 1024;
    private const string ServerLogFile = "ServerErrors.log";
    #endregion

    #region fields
    private AsyncServer server;
    private MainWindow window;
    private int selectedRoomIndex;
    private RoomViewModel selectedRoom;
    #endregion

    #region properties
    public AsyncClient Client { get; private set; }
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

    #region events
    public event EventHandler ClientCreated;
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
    #endregion

    #region constructors
    public MainWindowViewModel(MainWindow mainWindow)
    {
      window = mainWindow;
      window.Closed += window_Closed;
      Rooms = new ObservableCollection<RoomViewModel>();
      AllUsers = new ObservableCollection<UserViewModel>();
      Dispatcher = Dispatcher.CurrentDispatcher;

      ClearTabs();

      EnableServerCommand = new Command(EnableServer, Obj => server == null);
      DisableServerCommand = new Command(DisableServer, Obj => server != null);
      ConnectCommand = new Command(Connect, Obj => Client == null);
      DisconnectCommand = new Command(Disconnect, Obj => Client != null);
      ExitCommand = new Command(Obj => window.Close());
      CreateRoomCommand = new Command(CreateRoom, Obj => Client != null);
      DeleteRoomCommand = new Command(DeleteRoom, Obj => Client != null);
      ExitFromRoomCommand = new Command(ExitFromRoom, Obj => Client != null);
      OpenFilesDialogCommand = new Command(OpenFilesDialog, Obj => Client != null);
      OpenAboutProgramCommand = new Command(OpenAboutProgram);
    }
    #endregion

    #region command methods
    public void EnableServer(object obj)
    {
      ServerDialog dialog = new ServerDialog(Settings.Current.Nick, 
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

          server = new AsyncServer(ServerLogFile);
          server.Start(dialog.Port, dialog.UsingIPv6Protocol);

          CreateClient(dialog.Nick);
          Client.Info.NickColor = dialog.NickColor;
          Client.Connect(new IPEndPoint((dialog.UsingIPv6Protocol) ? IPAddress.IPv6Loopback : IPAddress.Loopback, dialog.Port));
        }
        catch (ArgumentException)
        {
          SelectedRoom.AddSystemMessage(ParamsError);

          if (server != null)
          {
            server.Dispose();
            server = null;
          }

          if (Client != null)
          {
            Client.Dispose();
            Client = null;
          }
        }
      }
    }

    public void DisableServer(object obj)
    {
      if (MessageBox.Show(ServerDisableQuestion, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
        return;

      if (Client != null)
      {
        Client.Dispose();
        Client = null;
      }

      server.Dispose();
      server = null;

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

        CreateClient(dialog.Nick);
        Client.Info.NickColor = dialog.NickColor;
        Client.Connect(new IPEndPoint(dialog.Address, dialog.Port));
      }
    }

    public void Disconnect(object obj)
    {
      try
      {
        Client.SendUnregisterRequest();
      }
      catch (SocketException) { }

      Client.Dispose();
      Client = null;

      ClearTabs();
    }

    public void CreateRoom(object obj)
    {
      try
      {
        CreateRoomDialog dialog = new CreateRoomDialog();
        if (dialog.ShowDialog() == true)
          Client.CreateRoom(dialog.RoomName);
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

        Client.DeleteRoom(SelectedRoom.Name);
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

        Client.ExitFormRoom(SelectedRoom.Name);
      }
      catch (SocketException se)
      {
        SelectedRoom.AddSystemMessage(se.Message);
      }
    }

    public void OpenFilesDialog(object obj)
    {
      PostedFilesDialog dialog = new PostedFilesDialog(Client);
      dialog.ShowDialog();
    }

    public void OpenAboutProgram(object obj)
    {
      AboutProgramDialog dialog = new AboutProgramDialog();
      dialog.ShowDialog();
    }
    #endregion

    #region client events
    private void Client_Connect(object sender, ConnectEventArgs e)
    {
      Dispatcher.Invoke(new Action<ConnectEventArgs>(args =>
      {
        if (args.Error != null)
        {
          SelectedRoom.AddSystemMessage(args.Error.Message);
          Client.Dispose();
          Client = null;
          return;
        }

        Client.SendRegisterRequest();
      }), e);
    }

    private void Client_Registration(object sender, RegistrationEventArgs e)
    {
      Dispatcher.Invoke(new Action<RegistrationEventArgs>(args =>
      {
        if (!args.Registered)
        {
          SelectedRoom.AddSystemMessage(RegFailNickAlreadyExist);

          Client.Dispose();
          Client = null;
        }
      }), e);
    }

    private void Client_ReceiveMessage(object sender, ReceiveMessageEventArgs e)
    {
      if (e.Type != MessageType.System && e.Type != MessageType.Private)
        return;

      Dispatcher.Invoke(new Action<ReceiveMessageEventArgs>(args =>
      {
        switch (args.Type)
        {
          case MessageType.Private:
            UserViewModel senderUser = AllUsers.Single(uvm => uvm.Info.Nick == args.Sender);
            UserViewModel receiverUser = AllUsers.Single(uvm => uvm.Info.Equals(Client.Info));
            SelectedRoom.AddPrivateMessage(senderUser, receiverUser, args.Message);
            break;

          case MessageType.System:
            SelectedRoom.AddSystemMessage(args.Message);
            break;
        }

        Alert();
      }), e);
    }

    private void Client_RoomRefreshed(object sender, RoomEventArgs e)
    {
      Dispatcher.Invoke(new Action<RoomEventArgs>(args =>
      {
        if (args.Room.Name == AsyncServer.MainRoomName)
        {
          AllUsers.Clear();

          foreach (User user in e.Room.Users)
          {
            if (user.Equals(Client.Info))
              AllUsers.Add(new UserViewModel(user, null) { IsClient = true });
            else
              AllUsers.Add(new UserViewModel(user, null));
          }
        }
      }), e);
    }

    private void Client_RoomOpened(object sender, RoomEventArgs e)
    {
      Dispatcher.Invoke(new Action<RoomEventArgs>(args =>
      {
        if (Rooms.FirstOrDefault(roomVM => roomVM.Name == args.Room.Name) != null)      
          return;
        
        RoomViewModel roomViewModel = new RoomViewModel(this, args.Room);
        roomViewModel.Updated = true;
        Rooms.Add(roomViewModel);

        window.Alert();
      }), e);
    }

    private void Client_RoomClosed(object sender, RoomEventArgs e)
    {
      Dispatcher.Invoke(new Action<RoomEventArgs>(args =>
      {
        RoomViewModel roomViewModel = Rooms.FirstOrDefault(roomVM => roomVM.Name == args.Room.Name);

        if (roomViewModel == null)
          return;

        Rooms.Remove(roomViewModel);
        window.Alert();
      }), e);
    }

    private void Client_AsyncError(object sender, AsyncErrorEventArgs e)
    {
      Dispatcher.Invoke(new Action<AsyncErrorEventArgs>(args =>
      {
        if (args.Error.GetType() == typeof(APINotSupprtedException))
        {
          Client.Dispose();
          Client = null;
        }

        SelectedRoom.AddSystemMessage(args.Error.Message);
      }), e);
    }
    #endregion

    #region helpers methods
    private void window_Closed(object sender, EventArgs e)
    {
      if (Client != null)
      {
        try
        {
          Client.SendUnregisterRequest();
        }
        catch (SocketException) { }

        Client.Dispose();
      }

      if (server != null)
        server.Dispose();

      Settings.SaveSettings();
    }

    private void OnClientCreated()
    {
      EventHandler temp = Interlocked.CompareExchange(ref ClientCreated, null, null);

      if (temp != null)
        temp(this, EventArgs.Empty);
    }

    public void Alert()
    {
      window.Alert();
    }

    private void CreateClient(string nick)
    {
      Client = new AsyncClient(nick);
      Client.Connected += Client_Connect;
      Client.ReceiveMessage += Client_ReceiveMessage;
      Client.ReceiveRegistrationResponse += Client_Registration;
      Client.RoomRefreshed += Client_RoomRefreshed;
      Client.AsyncError += Client_AsyncError;
      Client.RoomClosed += Client_RoomClosed;
      Client.RoomOpened += Client_RoomOpened;

      OnClientCreated();
    }

    private void ClearTabs()
    {
      AllUsers.Clear();
      Rooms.Clear();
      Rooms.Add(new RoomViewModel(this, new Room(null, AsyncServer.MainRoomName)));
      SelectedRoomIndex = 0;
    }
    #endregion
  }
}