using Engine.Helpers;
using Engine.Network.Connections;
using System;
using System.IO;
using System.Security;

namespace Engine.Network
{
  public struct Packed : IPoolable
  {
    private readonly Packer owner;
    private readonly MemoryStream stream;
    
    public byte[] Data
    {
      [SecurityCritical]
      get { return stream.GetBuffer(); }
    }
    
    public int Length
    {
      [SecurityCritical]
      get { return (int)stream.Length; }
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
    }
    
    [SecuritySafeCritical]
    public void Dispose()
    {
      owner.Release(this);
    }
  }
  
  public struct Unpacked<T> : IPoolable
    where T : IPackage
  {
    private readonly Packer owner;
    private readonly MemoryStream stream;
    
    public T Package
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }
    
    public byte[] RawData
    {
      [SecurityCritical]
      get
      {
        if (stream == null)
          return null;
        return stream.GetBuffer();
      }
    }
    
    public int RawLength
    {
      [SecurityCritical]
      get
      {
        if (stream == null)
          return 0;
        return (int) stream.Length;
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
      Package = package;
      stream = rawDataStream;
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      owner.Release(this);
    }
  }

  public interface IPoolable : IDisposable
  {
    MemoryStream Stream { get; }
  }

  public class Packer
  {
    public const int HeadSize = sizeof(int) + sizeof(bool);
    public const int LengthHead = 0;
    public const int EncryptionHead = sizeof(int);

    private Pool pool;
    private volatile byte[] key;

    [SecurityCritical]
    public Packer()
    {
      pool = new Pool(500);
    }

    [SecurityCritical]
    public void SetKey(byte[] symmetricKey)
    {
      if (key != null)
        throw new InvalidOperationException("Key already set");
      if (symmetricKey == null)
        throw new ArgumentNullException("symmetricKey");
      key = symmetricKey;
    }

    [SecurityCritical]
    public Packed Pack<T>(T package)
      where T : IPackage
    {
      return Pack(package, null);
    }

    [SecurityCritical]
    public Packed Pack<T>(T package, byte[] rawData)
      where T : IPackage
    {
      var encrypt = key != null;
      var streamCapacity = rawData == null ? (int?)null : rawData.Length;
      var stream = pool.Get(streamCapacity);

      stream.Write(BitConverter.GetBytes(0), 0, sizeof(int));
      stream.Write(BitConverter.GetBytes(encrypt), 0, sizeof(bool));

      using (var guard = pool.GetWithGuard())
      {
        Serializer.Serialize(guard.Stream, package);
        if (rawData != null)
          guard.Stream.Write(rawData, 0, rawData.Length);

        if (encrypt)
        {
          guard.Stream.Position = 0;
          using (var crypter = new Crypter())
          {
            crypter.SetKey(key);
            crypter.Encrypt(guard.Stream, stream);
          }
        }
        else
        {
          var guardStreamBuffer = guard.Stream.GetBuffer();
          stream.Write(guardStreamBuffer, 0, (int)guard.Stream.Length);
        }
      }

      var size = (int) stream.Length;
      var buffer = stream.GetBuffer();
      var lengthBlob = BitConverter.GetBytes(size);
      Buffer.BlockCopy(lengthBlob, 0, buffer, 0, lengthBlob.Length);

      return new Packed(this, stream);
    }

    [SecurityCritical]
    public Unpacked<T> Unpack<T>(byte[] data)
      where T : IPackage
    {
      return Unpack<T>(new MemoryStream(data, 0, data.Length, true, true));
    }

    [SecurityCritical]
    public Unpacked<T> Unpack<T>(MemoryStream input)
      where T : IPackage
    {
      var size = GetPackageSize(input);
      if (size < 0 || size > input.Length)
        throw new ArgumentException("The message is not read to the end");

      T result;
      MemoryStream rawData = null;

      using (var inputGuard = pool.GetWithGuard())
      using (var outputGuard = pool.GetWithGuard())
      {
        if (IsPackageEncrypted(input))
        {
          if (key == null)
            throw new InvalidOperationException("Key not set yet");

          var inputBuffer = input.GetBuffer();
          inputGuard.Stream.Write(inputBuffer, 0, size);
          inputGuard.Stream.Position = HeadSize;

          using (var crypter = new Crypter())
          {
            crypter.SetKey(key);
            crypter.Decrypt(inputGuard.Stream, outputGuard.Stream);
          }

          outputGuard.Stream.Position = 0;
        }
        else
        {
          var inputBuffer = input.GetBuffer();
          outputGuard.Stream.Write(inputBuffer, 0, size);
          outputGuard.Stream.Position = HeadSize;
        }

        result = Serializer.Deserialize<T>(outputGuard.Stream);
        if (outputGuard.Stream.Position < outputGuard.Stream.Length)
        {
          var outputBuffer = outputGuard.Stream.GetBuffer();
          var position = (int)outputGuard.Stream.Position;
          var length = (int)outputGuard.Stream.Length;
          var rawDataSize = length - position;

          rawData = pool.Get(rawDataSize);
          rawData.Write(outputBuffer, position, rawDataSize);
        }
      }

      return new Unpacked<T>(this, result, rawData);
    }

    [SecurityCritical]
    public bool IsPackageReceived(MemoryStream input)
    {
      var size = GetPackageSize(input);
      if (size == -1)
        return false;

      if (size > input.Length)
        return false;

      return true;
    }

    [SecurityCritical]
    public bool IsPackageEncrypted(MemoryStream input)
    {
      if (input.Length < HeadSize)
        return false;

      var buffer = input.GetBuffer();
      return BitConverter.ToBoolean(buffer, EncryptionHead);
    }

    [SecurityCritical]
    public int GetPackageSize(MemoryStream input)
    {
      if (input.Length < HeadSize)
        return -1;

      var buffer = input.GetBuffer();
      return BitConverter.ToInt32(buffer, LengthHead);
    }

    [SecurityCritical]
    public void Release<T>(T poolable)
      where T : IPoolable
    {
      if (poolable.Stream != null)
        pool.Put(poolable.Stream);
    }
  }
}
