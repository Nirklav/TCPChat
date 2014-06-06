using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{
  public class AsyncErrorEventArgs : EventArgs
  {
    public Exception Error { get; set; }
  }
}
