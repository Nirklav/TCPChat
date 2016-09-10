using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Model.Client.Entities
{
  [Serializable]
  public class ClientRoom : Room
  {
    [SecurityCritical]
    public ClientRoom(string admin, string name)
      : base(admin, name)
    {

    }
  }
}
