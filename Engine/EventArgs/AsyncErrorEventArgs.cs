using System;

namespace Engine
{
  [Serializable]
  public class AsyncErrorEventArgs : EventArgs
  {
    public Exception Error { get; set; }
  }
}
