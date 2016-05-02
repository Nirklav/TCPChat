using Engine.Model.Entities;
using System;

namespace Engine
{
  [Serializable]
  public class RegistrationEventArgs : EventArgs
  {
    public bool Registered { get; set; }
    public MessageId Message { get; set; }
  }
}
