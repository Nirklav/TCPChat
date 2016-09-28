using Engine.Network;
using System.IO;
using System.Security;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Helpers
{
  public static class Serializer
  {
    [SecuritySafeCritical]
    public static byte[] Serialize<T>(T obj)
    {
      using (MemoryStream stream = new MemoryStream())
      {
        BinSerializer.Serialize(stream, obj);
        return stream.ToArray();
      }
    }

    [SecuritySafeCritical]
    public static void Serialize<T>(Stream stream, T obj)
    {
      BinSerializer.Serialize(stream, obj);
    }

    [SecuritySafeCritical]
    public static T Deserialize<T>(byte[] message)
    {
      using (MemoryStream stream = new MemoryStream(message))
        return BinSerializer.Deserialize<T>(stream);
    }

    [SecuritySafeCritical]
    public static T Deserialize<T>(Stream stream)
    {
      return BinSerializer.Deserialize<T>(stream);
    }
  }
}
