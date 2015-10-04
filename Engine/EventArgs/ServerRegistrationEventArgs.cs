using System;

namespace Engine
{
  [Serializable]
  public class ServerRegistrationEventArgs : EventArgs
  {
    public string Nick { get; set; }
  }
}
