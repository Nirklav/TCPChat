using System;

namespace Engine
{
  [Serializable]
  public class PackageSendedEventArgs : EventArgs
  {
    public int Size { get; private set; }
    public Exception Exception { get; private set; }

    public PackageSendedEventArgs(int size)
    {
      Size = size;
    }

    public PackageSendedEventArgs(Exception e)
    {
      Exception = e;
    }
  }
}
