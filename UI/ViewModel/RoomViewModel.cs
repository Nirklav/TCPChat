using Engine;
using Engine.Api.Client.Admin;
using Engine.Api.Client.Files;
using Engine.Api.Client.Messages;
using Engine.Api.Client.Rooms;
using Engine.Api.Server.Admin;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Input;
using UI.Dialogs;
using UI.Infrastructure;
using DialogResult = System.Windows.Forms.DialogResult;
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

    private const string FileDialogFilter = "All files|*.*";

    private const int MessagesLimit = 200;
    private const int ShrinkSize = 100;
    #endregion

    #region fields
    private MainViewModel _mainViewModel;

    private bool _updated;
    private bool _enabled;

    private LightUserViewModel _allInRoomReceiver;
    private LightUserViewModel _selectedReceiver;
    private List<LightUserViewModel> _recivers;

    private bool _messagesAutoScroll;
    private string _message;
    private long? _messageId;
    private int _messageCaretIndex;
    private ObservableCollection<MessageViewModel> _messages;
    private HashSet<long> _messageIds;
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
      get { return _updated; }
      set { SetValue(value, nameof(Updated), v => _updated = v); }
    }

    public bool Enabled
    {
      get { return _enabled; }
      set { SetValue(value, nameof(Enabled), v => _enabled = v); }
    }

    public bool MessagesAutoScroll
    {
      get { return _messagesAutoScroll; }
      set
      {
        _messagesAutoScroll = value;
        OnPropertyChanged(nameof(MessagesAutoScroll));

        if (value)
          MessagesAutoScroll = false;
      }
    }

    public string Message
    {
      get { return _message; }
      set
      {
        SetValue(value, nameof(Message), v => _message = v);
        if (string.IsNullOrEmpty(value))
          SelectedMessageId = null;
      }
    }

    private long? SelectedMessageId
    {
      get { return _messageId; }
      set { SetValue(value, nameof(IsMessageSelected), v => _messageId = v); }
    }

    public bool IsMessageSelected
    {
      get { return SelectedMessageId != null; }
    }
    
    public int MessageCaretIndex
    {
      get { return _messageCaretIndex; }
      set { SetValue(value, nameof(MessageCaretIndex), v => _messageCaretIndex = v); }
    }

    public ObservableCollection<MessageViewModel> Messages
    {
      get { return _messages; }
      set { SetValue(value, nameof(Messages), v => _messages = v); }
    }

    public LightUserViewModel SelectedReceiver
    {
      get { return _selectedReceiver; }
      set { SetValue(value, nameof(SelectedReceiver), v => _selectedReceiver = v); }
    }

    public IEnumerable<LightUserViewModel> Receivers
    {
      get
      {
        yield return _allInRoomReceiver;
        foreach (var user in _recivers)
          yield return user;
      }
    }
    #endregion

    #region constructors
    public RoomViewModel(MainViewModel main)
      : base(main, true)
    {
      Init(main, null);

      Name = ServerChat.MainRoomName;
      Type = RoomType.Chat;
      Enabled = true;
    }

    public RoomViewModel(MainViewModel main, string roomName)
      : base(main, true)
    {
      using (var client = ClientModel.Get())
      {
        var room = client.Chat.GetRoom(roomName);

        Init(main, room.Users);

        Name = room.Name;
        Type = room is VoiceRoom ? RoomType.Voice : RoomType.Chat;
        Enabled = room.Enabled;

        FillMessages(client);
        RefreshReceivers(client);
      }
    }

    private void Init(MainViewModel main, IEnumerable<UserId> userIds)
    {
      _mainViewModel = main;
      Messages = new ObservableCollection<MessageViewModel>();
      SelectedReceiver = _allInRoomReceiver = new LightUserViewModel(AllInRoomKey, this);
      _recivers = new List<LightUserViewModel>();
      _messageIds = new HashSet<long>();

      var userViewModels = userIds == null
        ? Enumerable.Empty<UserViewModel>()
        : userIds.Select(id => new UserViewModel(id, this));
      Users = new ObservableCollection<UserViewModel>(userViewModels);

      SendMessageCommand = new Command(SendMessage, _ => ClientModel.Api != null && ClientModel.Client.IsConnected);
      PastReturnCommand = new Command(PastReturn);
      AddFileCommand = new Command(AddFile, _ => ClientModel.Api != null);
      InviteInRoomCommand = new Command(InviteInRoom, _ => ClientModel.Api != null);
      KickFromRoomCommand = new Command(KickFromRoom, _ => ClientModel.Api != null);
      ClearSelectedMessageCommand = new Command(ClearSelectedMessage);
      EnableVoiceCommand = new Command(EnableVoice, _ => Type == RoomType.Voice && !Enabled);
      DisableVoiceCommand = new Command(DisableVoice, _ => Type == RoomType.Voice && Enabled);

      Events.ReceiveMessage += CreateSubscriber<ReceiveMessageEventArgs>(ClientReceiveMessage);
      Events.RoomOpened += CreateSubscriber<RoomOpenedEventArgs>(ClientRoomOpened);
      Events.RoomRefreshed += CreateSubscriber<RoomRefreshedEventArgs>(ClientRoomRefreshed);
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

    public void AddMessage(long messageId, DateTime messageTime, UserId sender, string message)
    {
      AddMessage(new MessageViewModel(messageId, messageTime, sender, UserId.Empty, message, false, this));
    }

    public void AddPrivateMessage(UserId senderId, UserId receiverId, string message)
    {
      AddMessage(new MessageViewModel(Room.SpecificMessageId, DateTime.UtcNow, senderId, receiverId, message, true, this));
    }

    public void AddFileMessage(DateTime messageTime, UserId senderId, FileId fileId)
    {
      AddMessage(new MessageViewModel(messageTime, senderId, fileId, this));
    }

    private void AddMessage(MessageViewModel message)
    {
      TryShrinkMessages();

      if (message.MessageId == Room.SpecificMessageId || _messageIds.Add(message.MessageId))
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

      _messageIds.ExceptWith(deletingMessages.Select(m => m.MessageId));
      Messages = new ObservableCollection<MessageViewModel>(leftMessages);
    }
    #endregion

    #region command methods
    private void SendMessage(object obj)
    {
      if (string.IsNullOrWhiteSpace(Message))
        return;

      try
      {
        if (ServerAdminCommand.IsTextCommand(Message))
        {
          ClientModel.Api.Perform(new ClientSendAdminAction(Settings.Current.AdminPassword, Message));
          AddSystemMessage(Message);
        }
        else if (SelectedReceiver == _allInRoomReceiver)
        {
          var action = SelectedMessageId == null
            ? new ClientSendMessageAction(Name, Message)
            : new ClientSendMessageAction(Name, SelectedMessageId.Value, Message);

          ClientModel.Api.Perform(action);
        }
        else
        {
          ClientModel.Api.Perform(new ClientSendPrivateMessageAction(SelectedReceiver.UserId, Message));
          AddPrivateMessage(ClientModel.Client.Id, SelectedReceiver.UserId, Message);
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
          ClientModel.Api.Perform(new ClientAddFileAction(Name, openDialog.FileName));
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
          var allUsers = client.Chat.GetUsers();
          var availableUsers = allUsers.Select(u => u.Id).Except(Users.Select(u => u.UserId));
          if (!availableUsers.Any())
          {
            AddSystemMessage(Localizer.Instance.Localize(NoBodyToInviteKey));
            return;
          }

          var dialog = new UsersOperationDialog(InviteInRoomTitleKey, availableUsers);
          if (dialog.ShowDialog() == true)
            ClientModel.Api.Perform(new ClientInviteUsersAction(Name, dialog.Users));
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
        var dialog = new UsersOperationDialog(KickFormRoomTitleKey, Users.Select(u => u.UserId));
        if (dialog.ShowDialog() == true)
          ClientModel.Api.Perform(new ClientKickUsersAction(Name, dialog.Users));
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
      using (var client = ClientModel.Get())
      {
        var room = client.Chat.GetRoom(Name);
        room.Enable();
      }
    }

    private void DisableVoice(object obj)
    {
      Enabled = false;
      using (var client = ClientModel.Get())
      {
        var room = client.Chat.GetRoom(Name);
        room.Disable();
      }
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

      if (Name != _mainViewModel.SelectedRoom.Name)
        Updated = true;

      _mainViewModel.Alert();
    }

    private void ClientRoomOpened(RoomOpenedEventArgs e)
    {
      using (var client = ClientModel.Get())
      {
        RefreshUsers(client);
        RefreshReceivers(client);
        FillMessages(client);
      }
    }

    private void ClientRoomRefreshed(RoomRefreshedEventArgs e)
    {
      using (var client = ClientModel.Get())
      {
        if (e.RoomName == Name)
        {
          RefreshUsers(client);
          RefreshMessages(client, e);
        }

        if (e.RoomName == ServerChat.MainRoomName)
          RefreshReceivers(client);
      }
    }
    #endregion

    #region helpers
    private void RefreshUsers(ClientGuard client)
    {
      foreach (var user in Users)
        user.Dispose();
      Users.Clear();

      var room = client.Chat.GetRoom(Name);
      foreach (var user in room.Users)
        Users.Add(new UserViewModel(user, this));

      OnPropertyChanged(nameof(Name));
      OnPropertyChanged(nameof(Users));
    }

    private void RefreshMessages(ClientGuard client, RoomRefreshedEventArgs e)
    {
      var room = client.Chat.GetRoom(Name);

      if (e.RemovedMessages != null && e.RemovedMessages.Count > 0)
      {
        for (int i = Messages.Count - 1; i >= 0; i--)
        {
          var message = Messages[i];
          if (e.RemovedMessages.Contains(message.MessageId))
            Messages.RemoveAt(i);
          message.Dispose();
        }
      }

      if (e.AddedMessages != null && e.AddedMessages.Count > 0)
      {
        foreach (var messageId in e.AddedMessages)
        {
          var message = room.GetMessage(messageId);
          AddMessage(message.Id, message.Time, message.Owner, message.Text);
        }
      }
    }

    private void RefreshReceivers(ClientGuard client)
    {
      _recivers.Clear();

      LightUserViewModel newReciver = null;
      var selectedReceiverId = _selectedReceiver == _allInRoomReceiver
        ? UserId.Empty
        : _selectedReceiver.UserId;

      foreach (var user in client.Chat.GetUsers())
      {
        if (user.Id == client.Chat.User.Id)
          continue;

        var receiver = new LightUserViewModel(user.Id, this);
        _recivers.Add(receiver);

        if (user.Id == selectedReceiverId)
        {
          newReciver = receiver;
        }
      }

      SelectedReceiver = newReciver == null
        ? _allInRoomReceiver
        : newReciver;

      OnPropertyChanged(nameof(Receivers));
    }

    private void FillMessages(ClientGuard client)
    {
      var room = client.Chat.GetRoom(Name);
      var ordered = room.Messages.OrderBy(m => m.Time);
      foreach (var msg in ordered)
        AddMessage(msg.Id, msg.Time, msg.Owner, msg.Text);
    }
    #endregion
  }
}
