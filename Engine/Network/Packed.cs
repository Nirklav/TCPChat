using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Engine.Network
{
  [Serializable]
  public struct Packed : IPoolable, ISerializable
  {
    private readonly Packer owner;
    private readonly MemoryStream stream;
    private readonly byte[] data;

    public byte[] Data
    {
      [SecuritySafeCritical]
      get { return data ?? stream.GetBuffer(); }
    }

    public int Length
    {
      [SecuritySafeCritical]
      get
      {
        if (data != null)
          return data.Length;
        return (int)stream.Length;
      }
    }

    MemoryStream IPoolable.Stream
    {
      [SecuritySafeCritical]
      get { return stream; }
    }

    [SecurityCritical]
    public Packed(Packer packer, MemoryStream dataStream)
    {
      owner = packer;
      stream = dataStream;
      data = null;
    }

    [SecurityCritical]
    public Packed(byte[] dataArray)
    {
      owner = null;
      stream = null;
      data = dataArray;
    }

    [SecurityCritical]
    private Packed(SerializationInfo info, StreamingContext context)
    {
      owner = null;
      stream = null;
      data = (byte[])info.GetValue("data", typeof(byte[]));
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
      byte[] dataArray;
      if (data == null)
        dataArray = data;
      else
      {
        dataArray = new byte[(int)stream.Length];
        Array.Copy(stream.GetBuffer(), dataArray, dataArray.Length);
      }

      info.AddValue("data", dataArray, typeof(byte[]));
    }
  }
}
