using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Engine.Helpers
{
  [SecuritySafeCritical]
  public sealed class Crypter : IDisposable
  {
    #region Nested types
    /// <summary>
    /// CryptoStream closes outputStream, this class avoid this behaviour
    /// </summary>
    private class NotDisposableStream : Stream
    {
      private Stream _real;

      public NotDisposableStream(Stream stream)
      {
        if (stream == null)
          throw new ArgumentNullException("stream");
        _real = stream;
      }

      public override bool CanRead { get { return _real.CanRead; } }
      public override bool CanSeek { get { return _real.CanSeek; } }
      public override bool CanWrite { get { return _real.CanWrite; } }
      public override long Length { get { return _real.Length; } }

      public override long Position
      {
        get { return _real.Position; }
        set { _real.Position = value; }
      }

      public override void Flush() { _real.Flush(); }
      public override int Read(byte[] buffer, int offset, int count) { return _real.Read(buffer, offset, count); }
      public override long Seek(long offset, SeekOrigin origin) { return _real.Seek(offset, origin); }
      public override void SetLength(long value) { _real.SetLength(value); }
      public override void Write(byte[] buffer, int offset, int count) { _real.Write(buffer, offset, count); }
    }
    #endregion

    #region Constats
    private const int BufferSize = 4096;
    #endregion

    #region Private Values
    private SymmetricAlgorithm _algorithm;
    private bool _disposed;
    #endregion

    #region Constructors
    /// <summary>
    /// Create the instance of crypter. With AES-256 CBC algorithm.
    /// </summary>
    public Crypter()
    {
      _algorithm = new AesCryptoServiceProvider()
      {
        KeySize = 256,
        Mode = CipherMode.CBC,
        Padding = PaddingMode.PKCS7
      };
    }

    /// <summary>
    /// Create the instance of crypter.
    /// </summary>
    /// <param name="algorithm">Symmetric algorithm.</param>
    [SecuritySafeCritical]
    public Crypter(SymmetricAlgorithm algorithm)
    {
      if (algorithm == null)
        throw new ArgumentNullException("algorithm");
      _algorithm = algorithm;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Generate key, and IV.
    /// </summary>
    /// <returns>Returns generated key.</returns>
    [SecuritySafeCritical]
    public byte[] GenerateKey()
    {
      ThrowIfDisposed();

      _algorithm.GenerateKey();
      _algorithm.GenerateIV();

      return _algorithm.Key;
    }

    /// <summary>
    /// Set key, and generate IV.
    /// </summary>
    /// <param name="key">Key.</param>
    [SecuritySafeCritical]
    public void SetKey(byte[] key)
    {
      ThrowIfDisposed();

      _algorithm.Key = key;
      _algorithm.GenerateIV();
    }

    /// <summary>
    /// Encrypt the stream.
    /// </summary>
    /// <param name="inputStream">Stream that be encrypted.</param>
    /// <param name="outputStream">Stream that be used to write encrypted data.</param>
    [SecuritySafeCritical]
    public void Encrypt(Stream inputStream, Stream outputStream)
    {
      ThrowIfDisposed();

      if (inputStream == null)
        throw new ArgumentNullException("Input stream is null");

      if (outputStream == null)
        throw new ArgumentNullException("Output stream is null");

      var outputWrapper = new NotDisposableStream(outputStream);

      using (var transform = _algorithm.CreateEncryptor())
      using (var encrypter = new CryptoStream(outputWrapper, transform, CryptoStreamMode.Write))
      using (var writer = new BinaryWriter(outputWrapper, Encoding.Unicode, true))
      {
        writer.Write(_algorithm.IV);

        var dataBuffer = new byte[BufferSize];
        while (inputStream.Position < inputStream.Length)
        {
          var readed = inputStream.Read(dataBuffer, 0, BufferSize);
          encrypter.Write(dataBuffer, 0, readed);
        }
      }
    }

    /// <summary>
    /// Decypt the stream.
    /// </summary>
    /// <param name="inputStream">Stream that be decrypted.</param>
    /// <param name="outputStream">Stream that be used to write decypted data.</param>
    [SecuritySafeCritical]
    public void Decrypt(Stream inputStream, Stream outputStream)
    {
      ThrowIfDisposed();

      if (inputStream == null)
        throw new ArgumentNullException("Input stream is null");

      if (outputStream == null)
        throw new ArgumentNullException("Output stream is null");

      var inputWrapper = new NotDisposableStream(inputStream);

      using (var reader = new BinaryReader(inputWrapper))
      {
        _algorithm.IV = reader.ReadBytes(_algorithm.BlockSize / 8);

        using (var transform = _algorithm.CreateDecryptor())
        using (var decryptor = new CryptoStream(inputWrapper, transform, CryptoStreamMode.Read))
        {
          var dataBuffer = new byte[BufferSize];
          while (inputWrapper.Position < inputWrapper.Length)
          {
            var readed = decryptor.Read(dataBuffer, 0, BufferSize);
            outputStream.Write(dataBuffer, 0, readed);
          }
        }
      }
    }
    #endregion

    #region IDisposable
    [SecuritySafeCritical]
    private void ThrowIfDisposed()
    {
      if (_disposed)
        throw new ObjectDisposedException("Object Disposed");
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_disposed == true)
        return;

      _algorithm.Clear();

      _disposed = true;
    }
    #endregion
  }
}
