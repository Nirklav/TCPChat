using Engine;
using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using UI.Infrastructure;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using DialogResult = System.Windows.Forms.DialogResult;

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

    private const string TimeFormat = "hh:mm";
    private const string SizeFormat = " ({0:#,##0.0} {1})";
    private const string FileDialogFilter = "Все файлы|*.*";
    #endregion

    #region fields
    private int progress;
    private string text;
    private int? fileId;
    private RoomViewModel parentRoom;
    #endregion

    #region properties
    public long MessageId { get; private set; }
    public string Title { get; private set; }
    public UserViewModel Sender { get; private set; }
    public UserViewModel Receiver { get; private set; }
    public MessageType Type { get; private set; }

    public string Text 
    {
      get { return text; }
      set { SetValue(value, "Text", v => text = v); }
    }

    public int Progress
    {
      get { return progress; }
      set { SetValue(value, "Progress", v => progress = v); }
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

    public MessageViewModel(DateTime messageTime, string senderNick, int fileId, RoomViewModel roomVm)
      : this(Room.SpecificMessageId, roomVm, true)
    {
      this.fileId = fileId;

      Sender = new UserViewModel(senderNick, parentRoom);
      Progress = 0;

      var localMessageTime = messageTime.ToLocalTime();
      Title = Localizer.Instance.Localize(FromKey, localMessageTime.ToString(TimeFormat));

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

      NotifierContext.DownloadProgress += CreateSubscriber<FileDownloadEventArgs>(ClientDownloadProgress);
      NotifierContext.PostedFileDeleted += CreateSubscriber<FileDownloadEventArgs>(ClientPostedFileDeleted);
    }

    public MessageViewModel(long messageId, DateTime messageTime, string senderNick, string receiverNick, string message, bool isPrivate, RoomViewModel room)
      : this(messageId, room, false)
    {
      Text = message;
      Sender = new UserViewModel(senderNick, room);
      Receiver = new UserViewModel(receiverNick, room);
      Type = isPrivate ? MessageType.Private : MessageType.Common;

      EditMessageCommand = new Command(EditMessage, _ => ClientModel.Client != null);

      var localMessageTime = messageTime.ToLocalTime();
      Title = Localizer.Instance.Localize(isPrivate ? PMFormKey : FromKey, localMessageTime.ToString(TimeFormat));
    }

    private MessageViewModel(long messageId, RoomViewModel room, bool initializeNotifier)
      : base(room, initializeNotifier)
    {
      MessageId = messageId;
      parentRoom = room;
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
      if (e.RoomName != parentRoom.Name || e.FileId != fileId || Progress == e.Progress)
        return;

      using (var client = ClientModel.Get())
      {
        var file = GetFile(client, fileId.Value);

        if (e.Progress < 100)
          Progress = e.Progress;
        else
        {
          Progress = 0;
          parentRoom.AddSystemMessage(string.Format("Загрузка файла \"{0}\" завершена.", file.Name));
        }
      }
    }

    private void ClientPostedFileDeleted(FileDownloadEventArgs e)
    {
      if (e.RoomName != parentRoom.Name || e.FileId != fileId)
        return;

      Progress = 0;
      fileId = null;
    }
    #endregion

    #region command methods
    private void DownloadFile(object obj)
    {
      if (fileId == null)
      {
        parentRoom.AddSystemMessage(Localizer.Instance.Localize(FileNotFoundKey));
        return;
      }

      using (var client = ClientModel.Get())
      {
        var file = GetFile(client, fileId.Value);
        try
        {
          var saveDialog = new SaveFileDialog();
          saveDialog.OverwritePrompt = false;
          saveDialog.Filter = FileDialogFilter;
          saveDialog.FileName = file.Name;

          if (saveDialog.ShowDialog() == DialogResult.OK)
            ClientModel.Api.DownloadFile(saveDialog.FileName, parentRoom.Name, file.Id);
        }
        catch (ModelException me)
        {
          if (me.Code != ErrorCode.FileAlreadyDownloading)
          {
            parentRoom.AddSystemMessage(Localizer.Instance.Localize(me.Code));
            return;
          }

          var msg = Localizer.Instance.Localize(CancelDownloadingQuestionKey);
          var result = MessageBox.Show(msg, MainViewModel.ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question);
          if (result == MessageBoxResult.Yes)
          {
            ClientModel.Api.CancelDownloading(file.Id, true);
            Progress = 0;
          }
        }
        catch (ArgumentException ae)
        {
          parentRoom.AddSystemMessage(ae.Message);
        }
        catch (SocketException se)
        {
          parentRoom.AddSystemMessage(se.Message);
        }
      }
    }

    private void EditMessage(object obj)
    {
      parentRoom.EditMessage(this);
    }
    #endregion

    #region Helpers
    private FileDescription GetFile(ClientContext client, int fileId)
    {
      var room = client.Rooms[parentRoom.Name];
      return room.Files.Find(f => f.Id == fileId);
    }
    #endregion
  }
}
