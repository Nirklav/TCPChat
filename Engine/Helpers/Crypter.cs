using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Security;
using System.Runtime.InteropServices;

namespace Engine.Helpers
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
    /// Создает экемпляр класса Crypter.
    /// </summary>
    public Crypter(SymmetricAlgorithm symmetricAlg)
    {
      if (symmetricAlg == null)
        throw new ArgumentNullException();

      cryptAlgorithm = symmetricAlg;

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
      ThrowIfDisposed();

      cryptAlgorithm.GenerateKey();
      cryptAlgorithm.GenerateIV();

      return cryptAlgorithm.Key;
    }

    /// <summary>
    /// Производит шифрование потока.
    /// </summary>
    /// <param name="inputStream">Поток, который будет зашифрован.</param>
    /// <param name="outputStream">Поток, в который будет записан результат шифрования.</param>
    public void EncryptStream(Stream inputStream, Stream outputStream)
    {
      ThrowIfDisposed();

      if (inputStream == null || outputStream == null)
        throw new ArgumentNullException("One of the arguments (or two) equals null");

      using (var transform = cryptAlgorithm.CreateEncryptor(cryptAlgorithm.Key, cryptAlgorithm.IV))
      {
        int maxBufferSizeValue = bufferCoefficient * ConstBufferCoefficient;

        using (var csEncrypt = new CryptoStream(outputStream, transform, CryptoStreamMode.Write))
        {
          outputStream.Write(BitConverter.GetBytes(inputStream.Length), 0, sizeof(long));
          outputStream.Write(cryptAlgorithm.IV, 0, cryptAlgorithm.BlockSize / 8);

          inputStream.Position = 0;
          var dataBuffer = new byte[maxBufferSizeValue];

          while (inputStream.Position < inputStream.Length)
          {
            int dataSize = (inputStream.Length - inputStream.Position > maxBufferSizeValue) ? maxBufferSizeValue : (int)(inputStream.Length - inputStream.Position);

            int writedDataSize = CalculateDataSize(dataSize, maxBufferSizeValue);

            inputStream.Read(dataBuffer, 0, dataSize);
            csEncrypt.Write(dataBuffer, 0, writedDataSize);
          }
        }
      }
    }

    /// <summary>
    /// Производит дешифрование потока.
    /// </summary>
    /// <param name="inputStream">Поток, который будет дешифрован.</param>
    /// <param name="outputStream">Поток, в который будет записан результат дешифрования.</param>
    /// <param name="Key">Ключ для дешифрования.</param>
    public void DecryptStream(Stream inputStream, Stream outputStream, byte[] Key)
    {
      ThrowIfDisposed();

      if (inputStream == null || outputStream == null || Key == null)
        throw new ArgumentNullException("One of the arguments (or all) equals null");

      int maxBufferSizeValue = bufferCoefficient * ConstBufferCoefficient;

      byte[] originFileLengthArray = new byte[sizeof(long)];
      byte[] iv = new byte[cryptAlgorithm.BlockSize / 8];

      inputStream.Read(originFileLengthArray, 0, sizeof(long));
      inputStream.Read(iv, 0, cryptAlgorithm.BlockSize / 8);

      long deltaLength = inputStream.Length - HeadSize - cryptAlgorithm.BlockSize / 8 - BitConverter.ToInt64(originFileLengthArray, 0);

      using (var transform = cryptAlgorithm.CreateDecryptor(Key, iv))
      {
        using (var csEncrypt = new CryptoStream(inputStream, transform, CryptoStreamMode.Read))
        {
          var dataBuffer = new byte[maxBufferSizeValue];

          while (inputStream.Position < inputStream.Length)
          {
            int dataSize = (inputStream.Length - inputStream.Position > maxBufferSizeValue) ? maxBufferSizeValue : (int)(inputStream.Length - inputStream.Position - deltaLength);

            csEncrypt.Read(dataBuffer, 0, dataSize);
            outputStream.Write(dataBuffer, 0, dataSize);
          }
        }
      }
    }
    #endregion

    #region private methods
    private int CalculateDataSize(int dataSize, int maxDataSize)
    {
      if (dataSize == maxDataSize)
        return dataSize;

      int blockSize = cryptAlgorithm.BlockSize / 8;
      dataSize = (int)Math.Ceiling((double)dataSize / blockSize) * blockSize;

      return dataSize;
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
