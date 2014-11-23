using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{
  [Serializable]
  public class RegistrationEventArgs : EventArgs
  {
    public bool Registered { get; set; }
    public string Message { get; set; }
  }
}
