using System;

namespace Engine.Network
{
  [Serializable]
  public class ConnectionInfo
  {
    public byte[] PublicKey { get; set; }
  }

  [Serializable]
  public class ServerConnectionInfo : ConnectionInfo
  {
    public string ApiName { get; set; }
  }
}
