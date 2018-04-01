using Engine.Model.Common.Entities;
using System;

namespace Engine
{
  [Serializable]
  public class ConnectionEventArgs : EventArgs
  {
    public UserId Id { get; private set; }

    public ConnectionEventArgs(UserId id)
    {
      Id = id;
    }
  }
}
