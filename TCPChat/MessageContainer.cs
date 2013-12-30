using System;
using System.ComponentModel;
using System.Threading;
using TCPChat.Engine;

namespace TCPChat
{
    public class MessageContainer : INotifyPropertyChanged
    {
        private const string From = "[{0}] от: ";
        private const string PMForm = "[{0}] ЛС от: ";
        private const string TimeFormat = "HH:mm:ss";
        private const string SizeFormat = " ({0:#,##0.0} {1})";
        private const string ByteStr = "байт";
        private const string KByteStr = "Кб";
        private const string MByteStr = "Мб";

        int progress;
        FileDescription file;

        public MessageContainer(string systemMessage)
        {
            Message = systemMessage;
            IsPrivate = false;
            IsSystem = true;
            IsFile = false;
        }

        public MessageContainer(UserContainer sender, string fileName, FileDescription fileDescription)
        {
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
            IsFile = true;
            IsPrivate = false;
            IsSystem = false;
        }

        public MessageContainer(UserContainer sender, UserContainer receiver, string message, bool isPrivate)
        {
            Message = message;
            Sender = sender;
            Receiver = receiver;
            IsPrivate = isPrivate;
            IsSystem = false;
            IsFile = false;
            if (isPrivate)
                Title = string.Format(PMForm, DateTime.Now.ToString(TimeFormat));
            else
                Title = string.Format(From, DateTime.Now.ToString(TimeFormat));
        }

        public string Title { get; set; }
        public string Message { get; set; }
        public UserContainer Sender { get; set; }
        public UserContainer Receiver { get; set; }

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

        public bool IsPrivate { get; set; }
        public bool IsSystem { get; set; }
        public bool IsFile { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler temp = Interlocked.CompareExchange<PropertyChangedEventHandler>(ref PropertyChanged, null, null);

            if (temp != null)
                temp(this, new PropertyChangedEventArgs(name));
        }
    }
}
