using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{
  public class DataReceivedEventArgs : EventArgs
  {
    public byte[] ReceivedData { get; set; }
    public Exception Error { get; set; }
  }
}
