using System;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Network
{
  public enum AlgorithmId
  {
    Aes256CBC = 0
  }

  [Serializable]
  [BinType("HandshakeRequest")]
  public class HandshakeRequest
  {
    [BinField("c")]
    public byte[] RawX509Certificate;
  }

  [Serializable]
  [BinType("HandshakeResponse")]
  public class HandshakeResponse
  {
    [BinField("c")]
    public byte[] RawX509Certificate;

    [BinField("a")]
    public AlgorithmId AlgorithmId;

    [BinField("k")]
    public byte[] EncryptedKey;
  }

  [Serializable]
  [BinType("ServerConnectionInfo", Version = 2)]
  public class ServerInfo
  {
    [BinField("a")]
    public string ApiName;
  }
}
