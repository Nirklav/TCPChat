using Engine.Network.Connections;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Engine.Network
{
  [Serializable]
  public struct Unpacked<T> : IPoolable, ISerializable
      where T : IPackage
  {
    private readonly Packer owner;
    private readonly MemoryStream stream;
    private readonly byte[] rawData;

    public T Package
    {
      [SecuritySafeCritical]
      get;
      [SecurityCritical]
      private set;
    }

    public byte[] RawData
    {
      [SecuritySafeCritical]
      get
      {
        if (rawData != null)
          return rawData;
        if (stream != null)
          return stream.GetBuffer();
        return null;
      }
    }

    public int RawLength
    {
      [SecuritySafeCritical]
      get
      {
        if (rawData != null)
          return rawData.Length;
        if (stream != null)
          return (int)stream.Length;
        return 0;
      }
    }

    MemoryStream IPoolable.Stream
    {
      [SecuritySafeCritical]
      get { return stream; }
    }

    [SecurityCritical]
    public Unpacked(Packer packer, T package, MemoryStream rawDataStream)
    {
      owner = packer;
      stream = rawDataStream;
      rawData = null;
      Package = package;
    }

    [SecurityCritical]
    public Unpacked(T package, byte[] rawDataArray)
    {
      owner = null;
      stream = null;
      rawData = rawDataArray;
      Package = package;
    }

    [SecurityCritical]
    private Unpacked(SerializationInfo info, StreamingContext context)
    {
      owner = null;
      stream = null;
      rawData = (byte[]) info.GetValue("rawData", typeof(byte[]));
      Package = (T) info.GetValue("Package", typeof(T));       
    }

    [SecurityCritical]
    public Unpacked<T> Copy()
    {
      if (rawData != null)
        return this;

      var rawDataArray = (byte[])null;
      if (stream != null)
      {
        rawDataArray = new byte[(int)stream.Length];
        Array.Copy(stream.GetBuffer(), rawDataArray, rawDataArray.Length);
      }

      return new Unpacked<T>(Package, null);
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (owner != null)
        owner.Release(this);
    }

    [SecurityCritical]
    [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("rawData", rawData, typeof(byte[]));
      info.AddValue("Package", Package, typeof(T));
    }
  }
}
