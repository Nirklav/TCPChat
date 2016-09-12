using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Model.Server.Entities
{
  [Serializable]
  public class ServerChat : Chat<User, Room, VoiceRoom>
  {
    [SecurityCritical]
    public ServerChat()
    {

    }
  }
}
