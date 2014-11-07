using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Engine.Helpers
{
  public static class Serializer
  {
    public static byte[] Serialize<T>(T obj)
    {
      using(MemoryStream stream = new MemoryStream())
      {
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, obj);
        return stream.ToArray();
      }
    }

    public static void Serialize<T>(T obj, MemoryStream stream)
    {
      BinaryFormatter formatter = new BinaryFormatter();
      formatter.Serialize(stream, obj);
    }

    public static T Deserialize<T>(byte[] message)
    {
      using (MemoryStream stream = new MemoryStream(message))
      {
        stream.Position = sizeof(ushort);
        BinaryFormatter formatter = new BinaryFormatter();
        T obj = (T)formatter.Deserialize(stream);
        return obj;
      }
    }
  }
}
