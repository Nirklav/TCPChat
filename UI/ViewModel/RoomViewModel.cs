using Engine;
using Engine.Model.Client;
using Engine.Model.Entities;
using Engine.Model.Server;
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
using DialogResult = System.Windows.Forms.DialogResult;
using Keys = System.Windows.Forms.Keys;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace UI.ViewModel
{
  public class RoomViewModel : BaseViewModel
  {
    #region consts
    private const string InviteInRoomTitleKey = "roomViewModel-inviteInRoomTitle";
    private const string KickFormRoomTitleKey = "roomViewModel-kickFormRoomTitle";
    private const string NoBodyToInviteKey = "roomViewModel-nobodyToInvite";
    private const string AllInRoomKey = "roomViewModel-allInRoom";

    private const string FileDialogFilter = "Все файлы|*.*";

    private const int MessagesLimit = 200;
    private const int ShrinkSize = 100;
    #endregion

    #region fields
    private MainViewModel mainViewModel;

    private bool updated;
    private bool enabled;

    private UserViewModel allInRoom;
    private UserViewModel selectedReceiver;
    private List<UserViewModel> recivers;

    private bool messagesAutoScroll;
    private string message;
    private long? messageId;
    private int messageCaretIndex;
    private ObservableCollection<MessageViewModel> messages;
    private HashSet<long> messageIds;
    #endregion

    #region commands
    public ICommand InviteInRoomCommand { get; private set; }
    public ICommand KickFromRoomCommand { get; private set; }
    public ICommand SendMessageCommand { get; private set; }
    public ICommand PastReturnCommand { get; private set; }
    public ICommand ClearSelectedMessageCommand { get; private set; }
    public ICommand AddFileCommand { get; private set; }
    public ICommand EnableVoiceCommand { get; private set; }
    public ICommand DisableVoiceCommand { get; private set; }
    #endregion

    #region properties
    public string Name { get; private set; }
    public RoomType Type { get; private set; }
    public ObservableCollection<UserViewModel> Users { get; private set; }

    public bool Updated
    {
      get { return updated; }
      set { SetValue(value, "Updated", v => updated = v); }
    }

    public bool Enabled
    {
      get { return enabled; }
      set { SetValue(value, "Enabled", v => enabled = v); }
    }

    public bool MessagesAutoScroll
    {
      get { return messagesAutoScroll; }
      set
      {
        messagesAutoScroll = value;
        OnPropertyChanged("MessagesAutoScroll");

        if (value)
          MessagesAutoScroll = false;
      }
    }

    public string Message
    {
      get { return message; }
      set
      {
        SetValue(value, "Message", v => message = v);
        if (string.IsNullOrEmpty(value))
          SelectedMessageId = null;
      }
    }

    private long? SelectedMessageId
    {
      get { return messageId; }
      set { SetValue(value, "IsMessageSelected", v => messageId = v); }
    }

    public bool IsMessageSelected
    {
      get { return SelectedMessageId != null; }
    }
    
    public int MessageCaretIndex
    {
      get { return messageCaretIndex; }
      set { SetValue(value, "MessageCaretIndex", v => messageCaretIndex = v); }
    }

    public ObservableCollection<MessageViewModel> Messages
    {
      get { return messages; }
      set { SetValue(value, "Messages", v => messages = v); }
    }

    public UserViewModel SelectedReceiver
    {
      get { return selectedReceiver; }
      set { SetValue(value, "SelectedReceiver", v => selectedReceiver = v); }
    }

    public IEnumerable<UserViewModel> Receivers
    {
      get
      {
        yield return allInRoom;
        foreach (var user in recivers)
          yield return user;
      }
    }
    #endregion

    #region constructors
    public RoomViewModel(MainViewModel main)
      : base(main, true)
    {
      Init(main, null);

      Name = ServerModel.MainRoomName;
      Type = RoomType.Chat;
      Enabled = true;
    }

    public RoomViewModel(MainViewModel main, string roomName, IList<string> usersNicks)
      : base(main, true)
    {
      Init(main, usersNicks);

      using (var client = ClientModel.Get())
      {
        Room room;
        if (!client.Rooms.TryGetValue(roomName, out room))
          throw new ArgumentException("roomName");

        Name = room.Name;
        Type = room.Type;
        Enabled = room.Enabled;

        FillMessages(client);
        RefreshReceivers(client);
      }
    }

    private void Init(MainViewModel main, IList<string> usersNicks)
    {
      mainViewModel = main;
      Messages = new ObservableCollection<MessageViewModel>();
      SelectedReceiver = allInRoom = new UserViewModel(AllInRoomKey, null, this);
      recivers = new List<UserViewModel>();
      messageIds = new HashSet<long>();

      var userViewModels = usersNicks == null
        ? Enumerable.Empty<UserViewModel>()
        : usersNicks.Select(user => new UserViewModel(user, this));
      Users = new ObservableCollection<UserViewModel>(userViewModels);

      SendMessageCommand = new Command(SendMessage, _ => ClientModel.Api != null && ClientModel.Client.IsConnected);
      PastReturnCommand = new Command(PastReturn);
      AddFileCommand = new Command(AddFile, _ => ClientModel.Api != null);
      InviteInRoomCommand = new Command(InviteInRoom, _ => ClientModel.Api != null);
      KickFromRoomCommand = new Command(KickFromRoom, _ => ClientModel.Api != null);
      ClearSelectedMessageCommand = new Command(ClearSelectedMessage);
      EnableVoiceCommand = new Command(EnableVoice, _ => Type == RoomType.Voice && !Enabled);
      DisableVoiceCommand = new Command(DisableVoice, _ => Type == RoomType.Voice && Enabled);

      NotifierContext.ReceiveMessage += CreateSubscriber<ReceiveMessageEventArgs>(ClientReceiveMessage);
      NotifierContext.RoomOpened += CreateSubscriber<RoomEventArgs>(ClientRoomOpened);
      NotifierContext.RoomRefreshed += CreateSubscriber<RoomEventArgs>(ClientRoomRefreshed);
    }

    protected override void DisposeManagedResources()
    {
      base.DisposeManagedResources();

      foreach (var user in Users)
        user.Dispose();
      Users.Clear();

      foreach (var message in Messages)
        message.Dispose(); 
      Messages.Clear();
    }
    #endregion

    #region methods
    public void EditMessage(MessageViewModel message)
    {
      SelectedMessageId = message.MessageId;
      Message = message.Text;
    }

    public void AddSystemMessage(string message)
    {
      AddMessage(new MessageViewModel(message, this));
    }

    public void AddMessage(long messageId, DateTime messageTime, string sender, string message)
    {
      AddMessage(new MessageViewModel(messageId, messageTime, sender, null, message, false, this));
    }

    public void AddPrivateMessage(string senderNick, string receiverNick, string message)
    {
      AddMessage(new MessageViewModel(Room.SpecificMessageId, DateTime.UtcNow, senderNick, receiverNick, message, true, this));
    }

    public void AddFileMessage(DateTime messageTime, string senderNick, FileId fileId)
    {
      AddMessage(new MessageViewModel(messageTime, senderNick, fileId, this));
    }

    private void AddMessage(MessageViewModel message)
    {
      TryShrinkMessages();

      if (message.MessageId == Room.SpecificMessageId || messageIds.Add(message.MessageId))
        Messages.Add(message);
      else
      {
        var existingMessage = Messages.First(m => m.MessageId == message.MessageId);
        existingMessage.Text = message.Text;
      }    

      MessagesAutoScroll = true;
    }

    private void TryShrinkMessages()
    {
      if (Messages.Count < MessagesLimit)
        return;

      for (int i = 0; i < ShrinkSize; i++)
        Messages[i].Dispose();

      var leftMessages = Messages.Skip(ShrinkSize);
      var deletingMessages = Messages.Take(ShrinkSize);

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
        if (SelectedReceiver.IsAllInRoom)
          ClientModel.Api.SendMessage(SelectedMessageId, Message, Name);
        else
        {
          ClientModel.Api.SendPrivateMessage(SelectedReceiver.Nick, Message);
          AddPrivateMessage(ClientModel.Client.Id, SelectedReceiver.Nick, Message);
        }
      }
      catch (SocketException se)
      {
        AddSystemMessage(se.Message);
      }
      finally
      {
        SelectedMessageId = null;
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
        var result = openDialog.ShowDialog();

        if (result == DialogResult.OK)
          ClientModel.Api.AddFileToRoom(Name, openDialog.FileName);
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
        using (var client = ClientModel.Get())
        {
          var availableUsers = client.Users.Keys.Except(Users.Select(u => u.Nick));
          if (!availableUsers.Any())
          {
            AddSystemMessage(Localizer.Instance.Localize(NoBodyToInviteKey));
            return;
          }

          var dialog = new UsersOperationDialog(InviteInRoomTitleKey, availableUsers);
          if (dialog.ShowDialog() == true)
            ClientModel.Api.InviteUsers(Name, dialog.Users);
        }
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
        var dialog = new UsersOperationDialog(KickFormRoomTitleKey, Users.Select(u => u.Nick));
        if (dialog.ShowDialog() == true)
          ClientModel.Api.KickUsers(Name, dialog.Users);
      }
      catch (SocketException se)
      {
        AddSystemMessage(se.Message);
      }
    }

    private void ClearSelectedMessage(object obj)
    {
      if (SelectedMessageId == null)
        return;

      SelectedMessageId = null;
      Message = string.Empty;
    }

    private void EnableVoice(object obj)
    {
      Enabled = true;
      ClientModel.Api.EnableVoiceRoom(Name);
    }

    private void DisableVoice(object obj)
    {
      Enabled = false;
      ClientModel.Api.DisableVoiceRoom(Name);
    }
    #endregion

    #region client events
    private void ClientReceiveMessage(ReceiveMessageEventArgs e)
    {
      if (e.RoomName != Name)
        return;

      switch (e.Type)
      {
        case MessageType.Common:
          AddMessage(e.MessageId, e.Time, e.Sender, e.Message);
          break;

        case MessageType.File:
          AddFileMessage(e.Time, e.Sender, e.FileId);
          break;
      }

      if (Name != mainViewModel.SelectedRoom.Name)
        Updated = true;

      mainViewModel.Alert();
    }

    private void ClientRoomOpened(RoomEventArgs e)
    {
      using (var client = ClientModel.Get())
      {
        RefreshUsers(client);
        RefreshReceivers(client);
        FillMessages(client);
      }
    }

    private void ClientRoomRefreshed(RoomEventArgs e)
    {
      using (var client = ClientModel.Get())
      {
        if (e.RoomName == Name)
          RefreshUsers(client);
        
        if (e.RoomName == ServerModel.MainRoomName)
          RefreshReceivers(client);
      }
    }
    #endregion

    #region helpers
    private void RefreshUsers(ClientContext client)
    {
      foreach (var user in Users)
        user.Dispose();
      Users.Clear();

      Room room;
      if (!client.Rooms.TryGetValue(Name, out room))
        throw new ArgumentException("e.RoomName");

      foreach (var user in room.Users)
        Users.Add(new UserViewModel(user, this));

      OnPropertyChanged("Name");
      OnPropertyChanged("Admin");
      OnPropertyChanged("Users");
    }

    private void RefreshReceivers(ClientContext client)
    {
      var selectedReceiverNick = selectedReceiver == allInRoom
        ? null
        : selectedReceiver.Nick;

      recivers.Clear();
      foreach (var user in client.Users.Values)
      {
        if (user.Nick == client.User.Nick)
          continue;

        var receiver = new UserViewModel(user.Nick, this);
        if (user.Nick == selectedReceiverNick)
          selectedReceiver = receiver;
        recivers.Add(receiver);
      }

      OnPropertyChanged("Receivers");
      OnPropertyChanged("SelectedReceiver");
    }

    private void FillMessages(ClientContext client)
    {
      Room room;
      if (!client.Rooms.TryGetValue(Name, out room))
        throw new ArgumentException("e.RoomName");

      var ordered = room.Messages.OrderBy(m => m.Time);
      foreach (var msg in ordered)
        AddMessage(msg.Id, msg.Time, msg.Owner, msg.Text);
    }
    #endregion
  }
}
