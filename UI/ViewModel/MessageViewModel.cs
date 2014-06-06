using Engine;
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
    private const string TimeFormat = "hh:mm:ss";
    private const string SizeFormat = " ({0:#,##0.0} {1})";
    private const string ByteStr = "байт";
    private const string KByteStr = "Кб";
    private const string MByteStr = "Мб";
    private const string FileNotFound = "Файл недоступен";
    private const string CantDownloadItsFile = "Нельзя скачивать свой файл.";
    private const string CancelDownloadingQuestion = "Вы уже загружаете этот файл. Вы хотите отменить загрузку?";
    private const string FileDialogFilter = "Все файлы|*.*";

    int progress;
    FileDescription file;
    RoomViewModel roomViewModel;
    #endregion

    #region properties
    public string Title { get; set; }
    public string Message { get; set; }
    public UserViewModel Sender { get; set; }
    public UserViewModel Receiver { get; set; }
    public MessageType Type { get; set; }

    public FileDescription File
    {
      get { return file; }
      set
      {
        file = value;
        OnPropertyChanged("File");
      }
    }

    public int Progress
    {
      get { return progress; }
      set
      {
        progress = value;
        OnPropertyChanged("Progress");
      }
    }
    #endregion

    #region commands
    public ICommand DownloadFileCommand { get; private set; }
    #endregion

    #region constructors
    public MessageViewModel(string systemMessage, RoomViewModel room)
      : this(room)
    {
      Message = systemMessage;
      Type = MessageType.System;
    }

    public MessageViewModel(UserViewModel sender, string fileName, FileDescription fileDescription, RoomViewModel room)
      : this(room)
    {
      ClientModel.DownloadProgress += ClientDownloadProgress;
      ClientModel.PostedFileDeleted += ClientPostedFileDeleted;

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
      
      Message = fileName + string.Format(SizeFormat, size, sizeDim);
      Type = MessageType.File;
      DownloadFileCommand = new Command(DownloadFile, Obj => ClientModel.Client != null);
    }

    public MessageViewModel(UserViewModel sender, UserViewModel receiver, string message, bool isPrivate, RoomViewModel room)
      : this(room)
    {
      Message = message;
      Sender = sender;
      Receiver = receiver;
      Type = isPrivate ? MessageType.Private : MessageType.Common;
      Title = string.Format(isPrivate ? PMForm : From, DateTime.Now.ToString(TimeFormat));
    }

    private MessageViewModel(RoomViewModel room)
    {
      roomViewModel = room;
    }
    #endregion

    #region client events
    private void ClientDownloadProgress(object sender, FileDownloadEventArgs e)
    {
      roomViewModel.MainViewModel.Dispatcher.Invoke(new Action<FileDownloadEventArgs>(args =>
      {
        if (args.RoomName != roomViewModel.Name || !args.File.Equals(File))
          return;

        if (args.Progress >= 100)
        {
          Progress = 0;
          roomViewModel.AddSystemMessage(string.Format("Загрузка файла \"{0}\" завершена.", args.File.Name));
        }
        else
        {
          Progress = args.Progress;
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
            throw new FileAlreadyDownloadingException(File);

          if (client.User.Equals(File.Owner))
            throw new ArgumentException(CantDownloadItsFile);
        }

        SaveFileDialog saveDialog = new SaveFileDialog();
        saveDialog.OverwritePrompt = false;
        saveDialog.Filter = FileDialogFilter;
        saveDialog.FileName = File.Name;

        if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
          ClientModel.API.DownloadFile(saveDialog.FileName, roomViewModel.Name, File);
      }
      catch (FileAlreadyDownloadingException)
      {
        if (MessageBox.Show(CancelDownloadingQuestion, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
    #endregion
  }
}
