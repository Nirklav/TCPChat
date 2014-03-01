using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Concrete
{
  public class ConnectEventArgs : EventArgs
  {
    public Exception Error { get; set; }
  }
}
