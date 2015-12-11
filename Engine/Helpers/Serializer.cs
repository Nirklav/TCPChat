using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;

namespace Engine.Helpers
{
  public static class Serializer
  {
    [SecuritySafeCritical]
    public static byte[] Serialize<T>(T obj)
    {
      using (MemoryStream stream = new MemoryStream())
      {
        var formatter = new BinaryFormatter();
        formatter.Serialize(stream, obj);
        return stream.ToArray();
      }
    }

    [SecuritySafeCritical]
    public static void Serialize<T>(Stream stream, T obj)
    {
      var formatter = new BinaryFormatter();
      formatter.Serialize(stream, obj);
    }

    [SecuritySafeCritical]
    public static T Deserialize<T>(byte[] message)
    {
      using (MemoryStream stream = new MemoryStream(message))
      {
        var formatter = new BinaryFormatter();
        return (T)formatter.Deserialize(stream);
      }
    }

    [SecuritySafeCritical]
    public static T Deserialize<T>(Stream stream)
    {
      var formatter = new BinaryFormatter();
      return (T)formatter.Deserialize(stream);
    }
  }
}
