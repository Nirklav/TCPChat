using System;

namespace Engine
{
  [Serializable]
  public class AsyncErrorEventArgs : EventArgs
  {
    public Exception Error { get; private set; }

    public AsyncErrorEventArgs(Exception e)
    {
      Error = e;
    }
  }
}
