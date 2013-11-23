using System;
using System.Collections.Generic;

namespace TCPChat.Engine
{
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] ReceivedData { get; set; }
        public Exception Error { get; set; }
    }

    public class DataSendedEventArgs : EventArgs
    {
        public int SendedDataCount { get; set; }
        public Exception Error { get; set; }
    }

    public class ConnectEventArgs : EventArgs
    {
        public Exception Error { get; set; }
    }

    public class ReceiveMessageEventArgs : EventArgs
    {
        public bool IsFileMessage { get; set; }
        public bool IsPrivateMessage { get; set; }
        public bool IsSystemMessage { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
        public string RoomName { get; set; }
        public object State { get; set; }
    }

    public class RoomEventArgs : EventArgs
    {
        public RoomDescription Room { get; set; }
    }

    public class RegistrationEventArgs : EventArgs
    {
        public bool Registered { get; set; }
    }

    public class AsyncErrorEventArgs : EventArgs
    {
        public Exception Error { get; set; }
    }

    public class FileDownloadEventArgs : EventArgs
    {
        public int Progress { get; set; }
        public FileDescription File { get; set; }
        public string RoomName { get; set; }
        public bool Canceled { get; set; }
    }
}
