using Engine.Model.Common.Entities;
using System;

namespace Engine
{
  [Serializable]
  public class RegistrationEventArgs : EventArgs
  {
    public bool Registered { get; set; }
    public SystemMessageId Message { get; set; }
  }
}
