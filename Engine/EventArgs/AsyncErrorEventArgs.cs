using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{
  [Serializable]
  public class AsyncErrorEventArgs : EventArgs
  {
    public Exception Error { get; set; }
  }
}
