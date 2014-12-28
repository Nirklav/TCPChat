using Engine;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
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

    private const int MessagesLimit = 200;
    private const int CountToDelete = 100;
    #endregion

    #region fields
    private bool updated;
    private bool messagesAutoScroll;
    private string message;
    private int messageCaretIndex;
    private UserViewModel allInRoom;

    private ObservableCollection<MessageViewModel> messages;
    private HashSet<long> messageIds;

    private long? editingMessageId;
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
    public MainViewModel MainViewModel { get; private set; }

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
      set { SetValue(value, "Updated", v => updated = v); }
    }

    public string Name
    {
      get { return Description.Name; }
    }

    public string Message
    {
      get { return message; }
      set { SetValue(value, "Message", v => message = v); }
    }

    public int MessageCaretIndex
    {
      get { return messageCaretIndex; }
      set { SetValue(value, "MessageCaretIndex", v => messageCaretIndex = v); }
    }

    public RoomType Type { get { return Description is VoiceRoom ? RoomType.Voice : RoomType.Chat; } }

    public IEnumerable<UserViewModel> Receivers
    {
      get
      {
        yield return allInRoom;
        foreach (var user in MainViewModel.AllUsers)
          if (!user.IsClient)
            yield return user;

        SelectedReceiver = allInRoom;
      }
    }

    public ObservableCollection<MessageViewModel> Messages
    {
      get { return messages; }
      set { SetValue(value, "Messages", v => messages = v); }
    }

    public ObservableCollection<UserViewModel> Users { get; private set; }
    #endregion

    #region constructors
    public RoomViewModel(MainViewModel mainViewModel, Room room, IList<User> users)
      : base(true)
    {
      Description = room;
      MainViewModel = mainViewModel;
      Messages = new ObservableCollection<MessageViewModel>();
      allInRoom = new UserViewModel(new User("Все в комнате", Color.Black), this);
      messageIds = new HashSet<long>();
      Users = new ObservableCollection<UserViewModel>(users == null
        ? Enumerable.Empty<UserViewModel>()
        : room.Users.Select(user => new UserViewModel(users.Single(u => u.Equals(user)), this)));

      SendMessageCommand = new Command(SendMessage, Obj => ClientModel.Client != null);
      PastReturnCommand = new Command(PastReturn);
      AddFileCommand = new Command(AddFile, Obj => ClientModel.Client != null);
      InviteInRoomCommand = new Command(InviteInRoom, Obj => ClientModel.Client != null);
      KickFromRoomCommand = new Command(KickFromRoom, Obj => ClientModel.Client != null);

      MainViewModel.AllUsers.CollectionChanged += AllUsersCollectionChanged;
      NotifierContext.ReceiveMessage += ClientReceiveMessage;
      NotifierContext.RoomRefreshed += ClientRoomRefreshed;
    }

    public override void Dispose()
    {
      base.Dispose();

      foreach (UserViewModel user in Users)
        user.Dispose();

      foreach (MessageViewModel message in Messages)
        message.Dispose();

      Users.Clear();
      Messages.Clear();

      MainViewModel.AllUsers.CollectionChanged -= AllUsersCollectionChanged;
      NotifierContext.ReceiveMessage -= ClientReceiveMessage;
      NotifierContext.RoomRefreshed -= ClientRoomRefreshed;
    }
    #endregion

    #region methods
    public void EditMessage(MessageViewModel message)
    {
      editingMessageId = message.MessageId;
      Message = message.Text;
    }

    public void AddSystemMessage(string message)
    {
      AddMessage(new MessageViewModel(message, this));
    }

    public void AddMessage(long messageId, UserViewModel sender, string message)
    {
      AddMessage(new MessageViewModel(messageId, sender, null, message, false, this));
    }

    public void AddPrivateMessage(UserViewModel sender, UserViewModel receiver, string message)
    {
      AddMessage(new MessageViewModel(Room.SpecificMessageId, sender, receiver, message, true, this));
    }

    public void AddFileMessage(long messageId, UserViewModel sender, FileDescription file)
    {
      AddMessage(new MessageViewModel(messageId, sender, file.Name, file, this));
    }

    private void AddMessage(MessageViewModel message)
    {
      TryClearMessages();

      MessageViewModel lastMessage = null;
      if (Messages.Count > 0)
        lastMessage = Messages[Messages.Count - 1];

      if (message.MessageId == Room.SpecificMessageId || messageIds.Add(message.MessageId))
      {
        if (lastMessage == null || !lastMessage.TryConcat(message))
          Messages.Add(message);
      }
      else
      {
        var existingMessage = Messages.First(m => m.MessageId == message.MessageId);
        existingMessage.Text = message.Text;
      }    

      MessagesAutoScroll = true;
    }

    private void TryClearMessages()
    {
      if (Messages.Count < MessagesLimit)
        return;

      for (int i = 0; i < CountToDelete; i++)
        Messages[i].Dispose();

      var leftMessages = Messages.Skip(CountToDelete);
      var deletingMessages = Messages.Take(CountToDelete);

      messageIds.ExceptWith(deletingMessages.Select(m => m.MessageId));
      Messages = new ObservableCollection<MessageViewModel>(leftMessages);
    }
    #endregion

    #region command methods
    private void SendMessage(object obj)
    {
      if (Message == string.Empty)
        return;

      try
      {
        if (ClientModel.API == null || !ClientModel.Client.IsConnected)
          return;

        if (ReferenceEquals(allInRoom, SelectedReceiver))
          ClientModel.API.SendMessage(editingMessageId, Message, Name);
        else
        {
          ClientModel.API.SendPrivateMessage(SelectedReceiver.Nick, Message);
          var sender = MainViewModel.AllUsers.Single(uvm => uvm.Info.Equals(ClientModel.Client.Id));
          AddPrivateMessage(sender, SelectedReceiver, Message);
        }
      }
      catch (SocketException se)
      {
        AddSystemMessage(se.Message);
      }
      finally
      {
        editingMessageId = null;
        Message = string.Empty;
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
        var openDialog = new OpenFileDialog();
        openDialog.Filter = FileDialogFilter;

        if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && ClientModel.API != null)
          ClientModel.API.AddFileToRoom(Name, openDialog.FileName);
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
        var availableUsers = MainViewModel.AllUsers.Except(Users);
        if (!availableUsers.Any())
        {
          AddSystemMessage(NoBodyToInvite);
          return;
        }

        var dialog = new UsersOperationDialog(InviteInRoomTitle, availableUsers);
        if (dialog.ShowDialog() == true && ClientModel.API != null)
          ClientModel.API.InviteUsers(Name, dialog.Users);
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
        var dialog = new UsersOperationDialog(KickFormRoomTitle, Users);
        if (dialog.ShowDialog() == true && ClientModel.API != null)
          ClientModel.API.KickUsers(Name, dialog.Users);
      }
      catch (SocketException se)
      {
        AddSystemMessage(se.Message);
      }
    }
    #endregion

    #region client methods
    private void AllUsersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      OnPropertyChanged("Receivers");
    }

    private void ClientReceiveMessage(object sender, ReceiveMessageEventArgs e)
    {
      if (e.RoomName != Name)
        return;

      MainViewModel.Dispatcher.Invoke(new Action<ReceiveMessageEventArgs>(args =>
      {
        var senderUser = MainViewModel.AllUsers.Single(uvm => uvm.Info.Nick == args.Sender);

        switch (args.Type)
        {
          case MessageType.Common:
            AddMessage(args.MessageId, senderUser, args.Message);
            break;

          case MessageType.File:
            AddFileMessage(args.MessageId, senderUser, (FileDescription)args.State);
            break;
        }

        if (Name != MainViewModel.SelectedRoom.Name)
          Updated = true;

        MainViewModel.Alert();
      }), e);
    }

    private void ClientRoomRefreshed(object sender, RoomEventArgs e)
    {
      if (e.Room.Name != Name)
        return;

      MainViewModel.Dispatcher.Invoke(new Action<RoomEventArgs>(args =>
      {
        Description = args.Room;

        foreach (var user in Users)
          user.Dispose();

        Users.Clear();

        foreach (string user in Description.Users)
          Users.Add(new UserViewModel(args.Users.Find(u => string.Equals(u.Nick, user)), this));

        OnPropertyChanged("Name");
        OnPropertyChanged("Admin");
        OnPropertyChanged("Users");
      }), e);
    }
    #endregion
  }
}
