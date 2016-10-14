using System;

namespace Engine
{
  [Serializable]
  public class ConnectionEventArgs : EventArgs
  {
    public string Id { get; private set; }

    public ConnectionEventArgs(string id)
    {
      Id = id;
    }
  }
}
