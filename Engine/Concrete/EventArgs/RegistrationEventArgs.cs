using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Concrete
{
  public class RegistrationEventArgs : EventArgs
  {
    public bool Registered { get; set; }
  }
}
