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

namespace UI.ViewModel
{
  public class MessageViewModel : BaseViewModel
  {
    #region fields
    private const string ProgramName = "TCPChat";
    private const string From = "[{0}] от: ";
    private const string PMForm = "[{0}] ЛС от: ";
    private const string TimeFormat = "hh:mm";
    private const string SizeFormat = " ({0:#,##0.0} {1})";
    private const string ByteStr = "байт";
    private const string KByteStr = "Кб";
    private const string MByteStr = "Мб";
    private const string FileNotFound = "Файл недоступен";
    private const string CantDownloadItsFile = "Нельзя скачивать свой файл.";
    private const string CancelDownloadingQuestion = "Вы уже загружаете этот файл. Вы хотите отменить загрузку?";
    private const string FileDialogFilter = "Все файлы|*.*";

    private int progress;
    private string text;
    private FileDescription file;
    private RoomViewModel roomViewModel;
    private DateTime time;
    #endregion

    #region properties
    public long MessageId { get; private set; }
    public string Title { get; set; }
    public UserViewModel Sender { get; set; }
    public UserViewModel Receiver { get; set; }
    public MessageType Type { get; set; }

    public string Text 
    {
      get { return text; }
      set { SetValue(value, "Text", v => text = v); }
    }

    public FileDescription File
    {
      get { return file; }
      set { SetValue(value, "File", v => file = v); }
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

    public MessageViewModel(UserViewModel sender, string fileName, FileDescription fileDescription, RoomViewModel room)
      : this(Room.SpecificMessageId, room, true)
    {
      NotifierContext.DownloadProgress += ClientDownloadProgress;
      NotifierContext.PostedFileDeleted += ClientPostedFileDeleted;

      Sender = sender;
      File = fileDescription;
      Progress = 0;
      Title = string.Format(From, DateTime.Now.ToString(TimeFormat));

      string sizeDim = string.Empty;
      float size = 0;

      if (fileDescription.Size < 1024)
      {
        sizeDim = ByteStr;
        size = fileDescription.Size;
      }

      if (fileDescription.Size >= 1024 && fileDescription.Size < 1024 * 1024)
      {
        sizeDim = KByteStr;
        size = fileDescription.Size / 1024.0f;
      }

      if (fileDescription.Size >= 1024 * 1024)
      {
        sizeDim = MByteStr;
        size = fileDescription.Size / (1024.0f * 1024.0f);
      }
      
      Text = fileName + string.Format(SizeFormat, size, sizeDim);
      Type = MessageType.File;
      DownloadFileCommand = new Command(DownloadFile, Obj => ClientModel.Client != null);
    }

    public MessageViewModel(long messageId, UserViewModel sender, UserViewModel receiver, string message, bool isPrivate, RoomViewModel room)
      : this(messageId, room, false)
    {
      Text = message;
      Sender = sender;
      Receiver = receiver;
      Type = isPrivate ? MessageType.Private : MessageType.Common;

      EditMessageCommand = new Command(EditMessage, Obj => ClientModel.Client != null);

      Title = string.Format(isPrivate ? PMForm : From, DateTime.Now.ToString(TimeFormat));
    }

    private MessageViewModel(long messageId, RoomViewModel room, bool initializeNotifier)
      : base(initializeNotifier)
    {
      MessageId = messageId;
      roomViewModel = room;
      time = DateTime.Now;
    }

    public override void Dispose()
    {
      base.Dispose();

      if (NotifierContext == null)
        return;

      NotifierContext.DownloadProgress -= ClientDownloadProgress;
      NotifierContext.PostedFileDeleted -= ClientPostedFileDeleted;
    }
    #endregion

    #region methods

    #endregion

    #region client events
    private void ClientDownloadProgress(object sender, FileDownloadEventArgs e)
    {
      roomViewModel.MainViewModel.Dispatcher.Invoke(new Action<FileDownloadEventArgs>(args =>
      {
        if (args.RoomName != roomViewModel.Name || !args.File.Equals(File))
          return;

        if (args.Progress < 100)
          Progress = args.Progress;
        else
        {
          Progress = 0;
          roomViewModel.AddSystemMessage(string.Format("Загрузка файла \"{0}\" завершена.", args.File.Name));
        }
      }), e);
    }

    private void ClientPostedFileDeleted(object sender, FileDownloadEventArgs e)
    {
      roomViewModel.MainViewModel.Dispatcher.Invoke(new Action<FileDownloadEventArgs>(args =>
      {
        if (args.RoomName != roomViewModel.Name || !args.File.Equals(File))
          return;

        Progress = 0;
        File = null;
      }), e);
    }
    #endregion

    #region command methods
    private void DownloadFile(object obj)
    {
      if (File == null)
      {
        roomViewModel.AddSystemMessage(FileNotFound);
        return;
      }

      try
      {
        using (var client = ClientModel.Get())
        {
          if (client.DownloadingFiles.Exists(dFile => dFile.File.Equals(File)))
            throw new ModelException(ErrorCode.FileAlreadyDownloading, File);

          if (client.User.Equals(File.Owner))
            throw new ArgumentException(CantDownloadItsFile);
        }

        var saveDialog = new SaveFileDialog();
        saveDialog.OverwritePrompt = false;
        saveDialog.Filter = FileDialogFilter;
        saveDialog.FileName = File.Name;

        if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && ClientModel.API != null)
          ClientModel.API.DownloadFile(saveDialog.FileName, roomViewModel.Name, File);
      }
      catch (ModelException me)
      {
        if (me.Code != ErrorCode.FileAlreadyDownloading)
        {
          roomViewModel.AddSystemMessage(me.Message);
          return;
        }

        bool result = MessageBox.Show(CancelDownloadingQuestion, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        if (result && ClientModel.API != null)
        {
          ClientModel.API.CancelDownloading(File, true);
          Progress = 0;
        }
      }
      catch (ArgumentException ae)
      {
        roomViewModel.AddSystemMessage(ae.Message);
      }
      catch (SocketException se)
      {
        roomViewModel.AddSystemMessage(se.Message);
      }
    }

    private void EditMessage(object obj)
    {
      roomViewModel.EditMessage(this);
    }
    #endregion
  }
}
