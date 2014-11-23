using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{
  [Serializable]
  public class ServerRegistrationEventArgs : EventArgs
  {
    public string Nick { get; set; }
  }
}
