using System.IO;
using System.Security;
using System.Security.Permissions;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Helpers
{
  public static class Serializer
  {
    [SecuritySafeCritical]
    [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
    [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
    public static byte[] Serialize<T>(T obj)
    {
      using (MemoryStream stream = new MemoryStream())
      {
        BinSerializer.Serialize(stream, obj);
        return stream.ToArray();
      }
    }

    [SecuritySafeCritical]
    [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
    [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
    public static void Serialize<T>(Stream stream, T obj)
    {
      BinSerializer.Serialize(stream, obj);
    }

    [SecuritySafeCritical]
    [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
    [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
    public static T Deserialize<T>(byte[] message)
    {
      using (MemoryStream stream = new MemoryStream(message))
        return BinSerializer.Deserialize<T>(stream);
    }

    [SecuritySafeCritical]
    [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
    [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
    public static T Deserialize<T>(Stream stream)
    {
      return BinSerializer.Deserialize<T>(stream);
    }
  }
}
