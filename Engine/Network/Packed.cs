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
    private readonly Packer _owner;
    private readonly MemoryStream _stream;
    private readonly byte[] _data;

    public byte[] Data
    {
      [SecuritySafeCritical]
      get { return _data ?? _stream.GetBuffer(); }
    }

    public int Length
    {
      [SecuritySafeCritical]
      get
      {
        if (_data != null)
          return _data.Length;
        return (int)_stream.Length;
      }
    }

    MemoryStream IPoolable.Stream
    {
      [SecuritySafeCritical]
      get { return _stream; }
    }

    [SecurityCritical]
    public Packed(Packer owner, MemoryStream stream)
    {
      _owner = owner;
      _stream = stream;
      _data = null;
    }

    [SecurityCritical]
    public Packed(byte[] data)
    {
      _owner = null;
      _stream = null;
      _data = data;
    }

    [SecurityCritical]
    private Packed(SerializationInfo info, StreamingContext context)
    {
      _owner = null;
      _stream = null;
      _data = (byte[])info.GetValue("_data", typeof(byte[]));
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_owner != null)
        _owner.Release(this);
    }

    [SecurityCritical]
    [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      byte[] data;
      if (_data != null)
        data = _data;
      else
      {
        data = new byte[(int)_stream.Length];
        Array.Copy(_stream.GetBuffer(), data, data.Length);
      }

      info.AddValue("_data", data, typeof(byte[]));
    }
  }
}
