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
    private readonly Packer _owner;
    private readonly MemoryStream _stream;

    private readonly T _package;
    private readonly byte[] _rawData;

    public T Package
    {
      [SecuritySafeCritical]
      get { return _package; }
    }

    public byte[] RawData
    {
      [SecuritySafeCritical]
      get
      {
        if (_rawData != null)
          return _rawData;
        if (_stream != null)
          return _stream.GetBuffer();
        return null;
      }
    }

    public int RawLength
    {
      [SecuritySafeCritical]
      get
      {
        if (_rawData != null)
          return _rawData.Length;
        if (_stream != null)
          return (int)_stream.Length;
        return 0;
      }
    }

    MemoryStream IPoolable.Stream
    {
      [SecuritySafeCritical]
      get { return _stream; }
    }

    [SecurityCritical]
    public Unpacked(Packer owner, T package, MemoryStream stream)
    {
      _owner = owner;
      _stream = stream;
      _rawData = null;
      _package = package;
    }

    [SecurityCritical]
    public Unpacked(T package, byte[] rawData)
    {
      _owner = null;
      _stream = null;
      _rawData = rawData;
      _package = package;
    }

    [SecurityCritical]
    private Unpacked(SerializationInfo info, StreamingContext context)
    {
      _owner = null;
      _stream = null;
      _rawData = (byte[]) info.GetValue("_rawData", typeof(byte[]));
      _package = (T) info.GetValue("_package", typeof(T));
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
      byte[] rawData = null;
      if (_rawData != null)
      {
        rawData = _rawData;
      }
      else if (_stream != null)
      {
        rawData = new byte[(int)_stream.Length];
        Array.Copy(_stream.GetBuffer(), rawData, rawData.Length);
      }

      info.AddValue("_rawData", rawData, typeof(byte[]));
      info.AddValue("_package", Package, typeof(T));
    }
  }
}
