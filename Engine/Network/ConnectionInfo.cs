using System;

namespace Engine.Network.Connections
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
