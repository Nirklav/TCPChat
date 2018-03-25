using Engine.Network;
using System;

namespace Engine
{
  [Serializable]
  public class ConnectEventArgs : EventArgs
  {
    public Exception Error { get; private set; }

    public ConnectEventArgs()
    {
    }

    public ConnectEventArgs(Exception e)
    {
      Error = e;
    }
  }
}
