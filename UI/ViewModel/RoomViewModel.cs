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

    private void Init(MainViewModel main, IEnumerable<string> usersNicks)
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
      if (string.IsNullOrWhiteSpace(Message))
        return;

      try
      {
        if (ServerAdminCommand.IsTextCommand(Message))
        {
          ClientModel.Api.Perform(new ClientSendAdminAction(Settings.Current.AdminPassword, Message));
          AddSystemMessage(Message);
        }
        else if (SelectedReceiver.IsAllInRoom)
        {
          var action = SelectedMessageId == null
            ? new ClientSendMessageAction(Name, Message)
            : new ClientSendMessageAction(Name, SelectedMessageId.Value, Message);

          ClientModel.Api.Perform(action);
        }
        else
        {
          ClientModel.Api.Perform(new ClientSendPrivateMessageAction(SelectedReceiver.Nick, Message));
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
          var availableUsers = allUsers.Select(u => u.Nick).Except(Users.Select(u => u.Nick));
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
        var dialog = new UsersOperationDialog(KickFormRoomTitleKey, Users.Select(u => u.Nick));
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

      if (Name != mainViewModel.SelectedRoom.Name)
        Updated = true;

      mainViewModel.Alert();
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

      OnPropertyChanged("Name");
      OnPropertyChanged("Admin");
      OnPropertyChanged("Users");
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
      recivers.Clear();

      var selectedReceiverNick = selectedReceiver == allInRoom
        ? null
        : selectedReceiver.Nick;
      var newReciver = (UserViewModel) null;

      foreach (var user in client.Chat.GetUsers())
      {
        if (user.Nick == client.Chat.User.Nick)
          continue;

        var receiver = new UserViewModel(user.Nick, this);
        recivers.Add(receiver);

        if (user.Nick == selectedReceiverNick)
          newReciver = receiver;
      }
      OnPropertyChanged("Receivers");

      selectedReceiver = newReciver == null
        ? allInRoom
        : newReciver;
      OnPropertyChanged("SelectedReceiver");
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
