using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Concrete
{
  public class DataReceivedEventArgs : EventArgs
  {
    public byte[] ReceivedData { get; set; }
    public Exception Error { get; set; }
  }
}
