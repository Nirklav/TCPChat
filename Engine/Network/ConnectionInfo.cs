using System;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Network
{
  [Serializable]
  [BinType("ConnectionInfo")]
  public class ConnectionInfo
  {
    [BinField("p")]
    public byte[] PublicKey;
  }

  [Serializable]
  [BinType("ServerConnectionInfo")]
  public class ServerConnectionInfo : ConnectionInfo
  {
    [BinField("a")]
    public string ApiName;
  }
}
