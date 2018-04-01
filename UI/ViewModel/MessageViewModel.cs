using Engine;
using Engine.Api.Client.Files;
using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using UI.Infrastructure;
using DialogResult = System.Windows.Forms.DialogResult;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace UI.ViewModel
{
  public class MessageViewModel : BaseViewModel
  {
    #region consts
    private const string FromKey = "messageViewModel-from";
    private const string PMFormKey = "messageViewModel-pmForm";
    private const string ByteStrKey = "messageViewModel-byteStr";
    private const string KByteStrKey = "messageViewModel-kbyteStr";
    private const string MByteStrKey = "messageViewModel-mbyteStr";
    private const string FileNotFoundKey = "messageViewModel-fileNotFound";
    private const string CantDownloadItsFileKey = "messageViewModel-cantDownloadItsFile";
    private const string CancelDownloadingQuestionKey = "messageViewModel-cancelDownloadingQuestion";
    private const string FileDownloadedKey = "messageViewModel-fileDownloaded";
    private const string FileRemoved = "messageViewModel-fileRemoved";

    private const string CurrentDayTimeFormat = "hh:mm";
    private const string TimeFormat = "dd/MM hh:mm";
    private const string SizeFormat = " ({0:#,##0.0} {1})";
    private const string FileDialogFilter = "All files|*.*";
    #endregion

    #region fields
    private int _progress;
    private string _text;
    private FileId? _fileId;
    private RoomViewModel _parentRoom;
    #endregion

    #region properties
    public long MessageId { get; private set; }
    public string Title { get; private set; }
    public LightUserViewModel Sender { get; private set; }
    public LightUserViewModel Receiver { get; private set; }
    public MessageType Type { get; private set; }

    public string Text 
    {
      get { return _text; }
      set { SetValue(value, nameof(Text), v => _text = v); }
    }

    public int Progress
    {
      get { return _progress; }
      set { SetValue(value, nameof(Progress), v => _progress = v); }
    }
    #endregion

    #region commands
    public ICommand DownloadFileCommand { get; private set; }
    public ICommand EditMessageCommand { get; private set; }
    #endregion

    #region constructors
    public MessageViewModel(string systemMessage, RoomViewModel room)
      : this(Room.SpecificMessageId, room, false)
    {
      Text = systemMessage;
      Type = MessageType.System;
    }

    public MessageViewModel(DateTime messageTime, UserId senderId, FileId fileId, RoomViewModel roomVm)
      : this(Room.SpecificMessageId, roomVm, true)
    {
      this._fileId = fileId;

      Sender = new LightUserViewModel(senderId, _parentRoom);
      Progress = 0;     
      Title = Localizer.Instance.Localize(FromKey, GetTimeStr(messageTime));

      var sizeDim = string.Empty;
      var size = 0L;

      using (var client = ClientModel.Get())
      {
        var file = GetFile(client, fileId);

        if (file.Size < 1024)
        {
          sizeDim = Localizer.Instance.Localize(ByteStrKey);
          size = file.Size;
        }

        if (file.Size >= 1024 && file.Size < 1024 * 1024)
        {
          sizeDim = Localizer.Instance.Localize(KByteStrKey);
          size = file.Size / 1024;
        }

        if (file.Size >= 1024 * 1024)
        {
          sizeDim = Localizer.Instance.Localize(MByteStrKey);
          size = file.Size / (1024 * 1024);
        }

        Text = file.Name + string.Format(SizeFormat, size, sizeDim);
      }
      
      Type = MessageType.File;
      DownloadFileCommand = new Command(DownloadFile, _ => ClientModel.Api != null);

      Events.DownloadProgress += CreateSubscriber<FileDownloadEventArgs>(ClientDownloadProgress);
      Events.PostedFileDeleted += CreateSubscriber<FileDownloadEventArgs>(ClientPostedFileDeleted);
    }

    public MessageViewModel(long messageId, DateTime messageTime, UserId senderId, UserId receiverId, string message, bool isPrivate, RoomViewModel room)
      : this(messageId, room, false)
    {
      Text = message;
      Title = Localizer.Instance.Localize(isPrivate ? PMFormKey : FromKey, GetTimeStr(messageTime));
      Sender = new LightUserViewModel(senderId, room);
      Receiver = new LightUserViewModel(receiverId, room);
      Type = isPrivate ? MessageType.Private : MessageType.Common;

      EditMessageCommand = new Command(EditMessage, _ => ClientModel.Client != null);
    }

    private MessageViewModel(long messageId, RoomViewModel room, bool initializeNotifier)
      : base(room, initializeNotifier)
    {
      MessageId = messageId;
      _parentRoom = room;
    }

    protected override void DisposeManagedResources()
    {
      base.DisposeManagedResources();

      if (Sender != null)
        Sender.Dispose();
      if (Receiver != null)
        Receiver.Dispose();
    }
    #endregion

    #region client events
    private void ClientDownloadProgress(FileDownloadEventArgs e)
    {
      if (e.RoomName != _parentRoom.Name || e.FileId != _fileId || Progress == e.Progress)
        return;

      using (var client = ClientModel.Get())
      {
        var file = GetFile(client, _fileId.Value);
        if (file != null)
        {
          if (e.Progress < 100)
            Progress = e.Progress;
          else
          {
            Progress = 0;
            _parentRoom.AddSystemMessage(Localizer.Instance.Localize(FileDownloadedKey, file.Name));
          }
        }
      }
    }

    private void ClientPostedFileDeleted(FileDownloadEventArgs e)
    {
      if (e.RoomName != _parentRoom.Name || e.FileId != _fileId)
        return;

      Progress = 0;
      _fileId = null;
    }
    #endregion

    #region command methods
    private void DownloadFile(object obj)
    {
      if (_fileId == null)
      {
        _parentRoom.AddSystemMessage(Localizer.Instance.Localize(FileNotFoundKey));
        return;
      }

      using (var client = ClientModel.Get())
      {
        try
        {
          var file = GetFile(client, _fileId.Value);

          // File removed
          if (file == null)
          {
            _parentRoom.AddSystemMessage(Localizer.Instance.Localize(FileRemoved));
            return;
          }

          // File already downloading
          if (client.Chat.IsFileDownloading(_fileId.Value))
          {
            var msg = Localizer.Instance.Localize(CancelDownloadingQuestionKey);
            var result = MessageBox.Show(msg, MainViewModel.ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
              client.Chat.CancelFileDownload(file.Id, true);
              Progress = 0;
            }
            return;
          }

          // Show save file dialog
          var saveDialog = new SaveFileDialog();
          saveDialog.OverwritePrompt = false;
          saveDialog.Filter = FileDialogFilter;
          saveDialog.FileName = file.Name;

          if (saveDialog.ShowDialog() == DialogResult.OK)
            ClientModel.Api.Perform(new ClientDownloadFileAction(_parentRoom.Name, file.Id, saveDialog.FileName));
        }
        catch (ModelException me)
        {
          _parentRoom.AddSystemMessage(Localizer.Instance.Localize(me.Code));
        }
        catch (ArgumentException ae)
        {
          _parentRoom.AddSystemMessage(ae.Message);
        }
        catch (SocketException se)
        {
          _parentRoom.AddSystemMessage(se.Message);
        }
      }
    }

    private void EditMessage(object obj)
    {
      _parentRoom.EditMessage(this);
    }
    #endregion

    #region Helpers
    private FileDescription GetFile(ClientGuard client, FileId fileId)
    {
      var room = client.Chat.GetRoom(_parentRoom.Name);
      return room.TryGetFile(fileId);
    }

    private string GetTimeStr(DateTime messageTime)
    {
      var localMessageTime = messageTime.ToLocalTime();
      switch (messageTime.Date)
      {
        case var d when d == DateTime.UtcNow.Date:
          return localMessageTime.ToString(CurrentDayTimeFormat);
        default:
          return localMessageTime.ToString(TimeFormat);
      }
    }
    #endregion
  }
}
