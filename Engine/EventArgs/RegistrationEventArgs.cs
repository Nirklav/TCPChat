using System;

namespace Engine
{
  [Serializable]
  public class RegistrationEventArgs : EventArgs
  {
    public bool Registered { get; set; }
    public string Message { get; set; }
  }
}
