using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Concrete
{
  public class AsyncErrorEventArgs : EventArgs
  {
    public Exception Error { get; set; }
  }
}
