using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{
  [Serializable]
  public class DataSendedEventArgs : EventArgs
  {
    public int SendedDataCount { get; set; }
    public Exception Error { get; set; }
  }
}
