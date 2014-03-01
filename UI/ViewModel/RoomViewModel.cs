using Engine.Concrete;
using Engine.Concrete.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Input;
using UI.Dialogs;
using UI.Infrastructure;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace UI.ViewModel
{
  public class RoomViewModel : BaseViewModel
  {
    #region consts
    private const string ProgramName = "TCPChat";
    private const string InviteInRoomTitle = "Пригласить в комнату";
    private const string KickFormRoomTitle = "Удалить из комнаты";
    private const string NoBodyToInvite = "Некого пригласить. Все и так в комнате.";
    private const string FileDialogFilter = "Все файлы|*.*";
    #endregion
    
    #region fields
    private bool updated;
    private bool messagesAutoScroll;
    private string message;
    private int messageCaretIndex;
    private UserViewModel allInRoom;
    #endregion
    
    #region commands
    public ICommand InviteInRoomCommand { get; private set; }
    public ICommand KickFromRoomCommand { get; private set; }
    public ICommand SendMessageCommand { get; private set; }
    public ICommand PastReturnCommand { get; private set; }
    public ICommand AddFileCommand { get; private set; }
    #endregion

    #region properties
    public Room Description { get; private set; }
    public UserViewModel SelectedReceiver { get; set; }
    public MainWindowViewModel MainViewModel { get; private set; }

    public bool MessagesAutoScroll
    {
      get { return messagesAutoScroll; }
      set
      {
        messagesAutoScroll = value;
        OnPropertyChanged("MessagesAutoScroll");

        if (value == true)
          MessagesAutoScroll = false;
      }
    }

    public bool Updated
    {
      get { return updated; }
      set
      {
        updated = value;
        OnPropertyChanged("Updated");
      }
    }

    public string Name
    {
      get { return Description.Name; }
    }

    public string Message
    {
      get { return message; }
      set
      {
        message = value;
        OnPropertyChanged("Message");
      }
    }

    public int MessageCaretIndex
    {
      get { return messageCaretIndex; }
      set
      {
        messageCaretIndex = value;
        OnPropertyChanged("MessageCaretIndex");
      }
    }

    public IEnumerable<UserViewModel> Receivers
    {
      get
      {
        yield return allInRoom;
        foreach (UserViewModel model in MainViewModel.AllUsers)
          if (!model.IsClient)
            yield return model;

        SelectedReceiver = allInRoom;
      }
    }

    public ObservableCollection<UserViewModel> Users { get; private set; }
    public ObservableCollection<MessageViewModel> Messages { get; private set; }
    #endregion

    #region constructors
    public RoomViewModel(MainWindowViewModel model, Room room)
    {
      Description = room;
      MainViewModel = model;
      Messages = new ObservableCollection<MessageViewModel>();
      Users = new ObservableCollection<UserViewModel>(room.Users.Select(user => new UserViewModel(user, this)));
      allInRoom = new UserViewModel(new User("Все в комнате"), this);

      SendMessageCommand = new Command(SendMessage, Obj => MainViewModel.Client != null);
      PastReturnCommand = new Command(PastReturn);
      AddFileCommand = new Command(AddFile, Obj => MainViewModel.Client != null);
      InviteInRoomCommand = new Command(InviteInRoom, Obj => MainViewModel.Client != null);
      KickFromRoomCommand = new Command(KickFromRoom, Obj => MainViewModel.Client != null);

      MainViewModel.ClientCreated += MainViewModel_ClientCreated;
      MainViewModel.AllUsers.CollectionChanged += AllUsers_CollectionChanged;

      if (MainViewModel.Client != null)
        RegisterMethods();
    }
    #endregion

    #region methods
    public void AddSystemMessage(string message)
    {
      Messages.Add(new MessageViewModel(message, this));
      MessagesAutoScroll = true;
    }

    public void AddMessage(UserViewModel sender, string message)
    {
      Messages.Add(new MessageViewModel(sender, null, message, false, this));
      MessagesAutoScroll = true;
    }

    public void AddPrivateMessage(UserViewModel sender, UserViewModel receiver, string message)
    {
      Messages.Add(new MessageViewModel(sender, receiver, message, true, this));
      MessagesAutoScroll = true;
    }

    public void AddFileMessage(UserViewModel sender, FileDescription file)
    {
      Messages.Add(new MessageViewModel(sender, file.Name, file, this));
      MessagesAutoScroll = true;
    }
    #endregion

    #region command methods
    private void SendMessage(object obj)
    {
      if (Message == string.Empty) return;

      try
      {
        if (ReferenceEquals(allInRoom, SelectedReceiver))
        {
          MainViewModel.Client.SendMessage(Message, Name);
        }
        else
        {
          MainViewModel.Client.SendPrivateMessage(SelectedReceiver.Nick, Message);
          AddPrivateMessage(MainViewModel.AllUsers.Single(uvm => uvm.Info.Equals(MainViewModel.Client.Info)), SelectedReceiver, Message);
        }

        Message = string.Empty;
      }
      catch (SocketException se)
      {
        AddSystemMessage(se.Message);
      }
    }

    private void PastReturn(object obj)
    {
      Message += Environment.NewLine;
      MessageCaretIndex = Message.Length;
    }

    private void AddFile(object obj)
    {
      try
      {
        OpenFileDialog openDialog = new OpenFileDialog();
        openDialog.Filter = FileDialogFilter;

        if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
          MainViewModel.Client.AddFileToRoom(Name, openDialog.FileName);
      }
      catch (SocketException se)
      {
        AddSystemMessage(se.Message);
      }
    }

    private void InviteInRoom(object obj)
    {
      try
      {
        IEnumerable<UserViewModel> availableUsers = MainViewModel.AllUsers.Except(Users);
        if (availableUsers.Count() == 0)
        {
          AddSystemMessage(NoBodyToInvite);
          return;
        }

        UsersOperationDialog dialog = new UsersOperationDialog(InviteInRoomTitle, availableUsers);
        if (dialog.ShowDialog() == true)
          MainViewModel.Client.InviteUsers(Name, dialog.Users);
      }
      catch (SocketException se)
      {
        AddSystemMessage(se.Message);
      }
    }

    private void KickFromRoom(object obj)
    {
      try
      {
        UsersOperationDialog dialog = new UsersOperationDialog(KickFormRoomTitle, Users);
        if (dialog.ShowDialog() == true)
          MainViewModel.Client.KickUsers(Name, dialog.Users);
      }
      catch (SocketException se)
      {
        AddSystemMessage(se.Message);
      }
    }
    #endregion

    #region client methods
    private void MainViewModel_ClientCreated(object sender, EventArgs e)
    {
      RegisterMethods();
    }

    private void AllUsers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      OnPropertyChanged("Receivers");
    }

    private void RegisterMethods()
    {
      MainViewModel.Client.ReceiveMessage += Client_ReceiveMessage;
      MainViewModel.Client.RoomRefreshed += Client_RoomRefreshed;
    }

    private void Client_ReceiveMessage(object sender, ReceiveMessageEventArgs e)
    {
      if (e.RoomName != Name)
        return;

      MainViewModel.Dispatcher.Invoke(new Action<ReceiveMessageEventArgs>(args =>
      {
        UserViewModel senderUser = MainViewModel.AllUsers.Single(uvm => uvm.Info.Nick == args.Sender);

        switch (args.Type)
        {
          case MessageType.Common:
            AddMessage(senderUser, args.Message);
            break;

          case MessageType.File:
            AddFileMessage(senderUser, (FileDescription)args.State);
            break;
        }

        if (Name != MainViewModel.SelectedRoom.Name)
          Updated = true;

        MainViewModel.Alert();
      }), e);
    }

    private void Client_RoomRefreshed(object sender, RoomEventArgs e)
    {
      if (e.Room.Name != Name)
        return;

      MainViewModel.Dispatcher.Invoke(new Action<RoomEventArgs>(args =>
      {
        Description = args.Room;
        Users.Clear();

        foreach (User user in Description.Users)
          Users.Add(new UserViewModel(user, this));

        OnPropertyChanged("Name");
        OnPropertyChanged("Admin");
        OnPropertyChanged("Users");
      }), e);
    }
    #endregion
  }
}
