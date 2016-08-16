using Engine.Helpers;
using Engine.Network.Connections;
using System;
using System.IO;
using System.Security;
using System.Threading;

namespace Engine.Network
{
  public interface IPoolable : IDisposable
  {
    MemoryStream Stream { get; }
  }

  public class Packer
  {
    private static long NotCryptedMessagesSent;
    private static long NotCryptedMessagesRecived;
    private static long CryptedMessagesSent;
    private static long CryptedMessagesRecived;

    private const int poolSize = 500;
    private static Pool pool;

    public const int HeadSize = sizeof(int) + sizeof(bool);
    public const int LengthHead = 0;
    public const int EncryptionHead = sizeof(int);
    
    private volatile byte[] key;

    [SecurityCritical]
    public Packer()
    {
      if (pool == null)
        pool = new Pool(poolSize);
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
      var resultCapacity = rawData == null ? (int?)null : rawData.Length;
      var result = pool.Get(resultCapacity);

      result.Write(BitConverter.GetBytes(0), 0, sizeof(int));
      result.Write(BitConverter.GetBytes(encrypt), 0, sizeof(bool));

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
            crypter.Encrypt(guard.Stream, result);
          }

          Interlocked.Increment(ref CryptedMessagesSent);
        }
        else
        {
          var guardStreamBuffer = guard.Stream.GetBuffer();
          result.Write(guardStreamBuffer, 0, (int)guard.Stream.Length);

          Interlocked.Increment(ref NotCryptedMessagesSent);
        }
      }

      var size = (int) result.Length;
      var buffer = result.GetBuffer();
      var lengthBlob = BitConverter.GetBytes(size);
      Buffer.BlockCopy(lengthBlob, 0, buffer, 0, lengthBlob.Length);

      return new Packed(this, result);
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

          Interlocked.Increment(ref CryptedMessagesRecived);
        }
        else
        {
          var inputBuffer = input.GetBuffer();
          outputGuard.Stream.Write(inputBuffer, 0, size);
          outputGuard.Stream.Position = HeadSize;

          Interlocked.Increment(ref NotCryptedMessagesRecived);
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
