using System;

namespace Engine
{
  [Serializable]
  public class DataSendedEventArgs : EventArgs
  {
    public int SendedDataCount { get; set; }
    public Exception Error { get; set; }
  }
}
