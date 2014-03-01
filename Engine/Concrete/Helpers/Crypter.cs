using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Security;
using System.Runtime.InteropServices;

namespace Engine.Concrete.Helpers
{
  public sealed class Crypter :
      IDisposable
  {
    #region Constats
    private const int HeadSize = 8;
    private const int ConstBufferCoefficient = 256 * 1024;
    #endregion

    #region Private Values
    private SymmetricAlgorithm cryptAlgorithm;
    private int bufferCoefficient;
    #endregion

    #region Properties
    /// <summary>
    /// Получает коэфициент, определяющий размер буффера (в пределах 10-100).
    /// </summary>
    public int BufferCoefficient
    {
      get
      {
        ThrowIfDisposed();

        return bufferCoefficient;
      }
      set
      {
        ThrowIfDisposed();

        bufferCoefficient = (value >= 10 && value <= 100) ? value : 20;
      }
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Создает экемпляр класса FileCrypter.
    /// </summary>
    public Crypter(SymmetricAlgorithm SymmetricAlg)
    {
      if (SymmetricAlg == null)
        throw new ArgumentNullException();

      cryptAlgorithm = SymmetricAlg;

      bufferCoefficient = 10;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Генерирует ключ и вектор инициализации.
    /// </summary>
    /// <returns>Ключ.</returns>
    public byte[] GenerateKey()
    {
      cryptAlgorithm.GenerateKey();
      cryptAlgorithm.GenerateIV();

      return cryptAlgorithm.Key;
    }

    /// <summary>
    /// Производит шифрование потока.
    /// </summary>
    /// <param name="input_file">Поток, который будет зашифрован.</param>
    /// <param name="output_file">Поток, в который будет записан результат шифрования.</param>
    public void EncryptStream(Stream InputStream, Stream OutputStream)
    {
      ThrowIfDisposed();

      if (InputStream == null || OutputStream == null)
        throw new ArgumentNullException("One of the arguments (or two) equals null");

      using (ICryptoTransform encryptor = cryptAlgorithm.CreateEncryptor(cryptAlgorithm.Key, cryptAlgorithm.IV))
      {
        int MaxBufferSizeValue = bufferCoefficient * ConstBufferCoefficient;

        CryptoStream csEncrypt = new CryptoStream(OutputStream, encryptor, CryptoStreamMode.Write);

        OutputStream.Write(BitConverter.GetBytes(InputStream.Length), 0, sizeof(long));
        OutputStream.Write(cryptAlgorithm.IV, 0, cryptAlgorithm.BlockSize / 8);

        InputStream.Position = 0;
        byte[] DataBuffer = new byte[MaxBufferSizeValue];

        while (InputStream.Position < InputStream.Length)
        {
          int DataSize = (InputStream.Length - InputStream.Position > MaxBufferSizeValue) ? MaxBufferSizeValue : (int)(InputStream.Length - InputStream.Position);

          int WritedDataSize = CalculateDataSize(DataSize, MaxBufferSizeValue);

          InputStream.Read(DataBuffer, 0, DataSize);
          csEncrypt.Write(DataBuffer, 0, WritedDataSize);
        }
      }
    }

    /// <summary>
    /// Производит дешифрование потока.
    /// </summary>
    /// <param name="input_file">Поток, который будет дешифрован.</param>
    /// <param name="output_file">Поток, в который будет записан результат дешифрования.</param>
    public void DecryptStream(Stream InputStream, Stream OutputStream, byte[] Key)
    {
      ThrowIfDisposed();

      if (InputStream == null || OutputStream == null || Key == null)
        throw new ArgumentNullException("One of the arguments (or all) equals null");

      int MaxBufferSizeValue = bufferCoefficient * ConstBufferCoefficient;

      byte[] OriginFileLengthArray = new byte[sizeof(long)];
      byte[] IV = new byte[cryptAlgorithm.BlockSize / 8];

      InputStream.Read(OriginFileLengthArray, 0, sizeof(long));
      InputStream.Read(IV, 0, cryptAlgorithm.BlockSize / 8);

      long DeltaLength = InputStream.Length - HeadSize - cryptAlgorithm.BlockSize / 8 - BitConverter.ToInt64(OriginFileLengthArray, 0);

      using (ICryptoTransform decryptor = cryptAlgorithm.CreateDecryptor(Key, IV))
      {
        CryptoStream csEncrypt = new CryptoStream(InputStream, decryptor, CryptoStreamMode.Read);

        byte[] DataBuffer = new byte[MaxBufferSizeValue];

        while (InputStream.Position < InputStream.Length)
        {
          int DataSize = (InputStream.Length - InputStream.Position > MaxBufferSizeValue) ? MaxBufferSizeValue : (int)(InputStream.Length - InputStream.Position - DeltaLength);

          csEncrypt.Read(DataBuffer, 0, DataSize);
          OutputStream.Write(DataBuffer, 0, DataSize);
        }
      }
    }
    #endregion

    #region private methods
    private int CalculateDataSize(int DataSize, int MaxDataSize)
    {
      if (DataSize == MaxDataSize)
        return DataSize;

      int BlockSize = cryptAlgorithm.BlockSize / 8;
      DataSize = (int)Math.Ceiling((double)DataSize / BlockSize) * BlockSize;

      return DataSize;
    }
    #endregion

    #region IDisposable
    private bool disposed = false;

    private void ThrowIfDisposed()
    {
      if (disposed)
        throw new ObjectDisposedException("Object Disposed");
    }

    public void Dispose()
    {
      if (disposed == true) return;

      cryptAlgorithm.Clear();

      disposed = true;
    }
    #endregion
  }
}
