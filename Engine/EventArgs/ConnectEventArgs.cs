using System;

namespace Engine
{
  [Serializable]
  public class ConnectEventArgs : EventArgs
  {
    public Exception Error { get; set; }
  }
}
