using System;

namespace Engine
{
  [Serializable]
  public class ServerRegistrationEventArgs : EventArgs
  {
    public string Nick { get; private set; }

    public ServerRegistrationEventArgs(string nick)
    {
      Nick = nick;
    }
  }
}
