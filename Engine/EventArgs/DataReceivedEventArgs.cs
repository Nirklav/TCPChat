using System;

namespace Engine
{
  [Serializable]
  public class DataReceivedEventArgs : EventArgs
  {
    public byte[] ReceivedData { get; set; }
    public Exception Error { get; set; }
  }
}
