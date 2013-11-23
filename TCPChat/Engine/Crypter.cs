using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Security;
using System.Runtime.InteropServices;

namespace TCPChat.Engine
{
    public sealed class Crypter :
        IDisposable
    {
        #region Constats
        private const int HeadSize = 8;
        #endregion

        #region Private Values
        private SymmetricAlgorithm CryptAlgorithm;
        private int BufferCoefficientValue;
        #endregion

        #region Properties
        /// <summary>
        /// Получает коэфициент, определяющий размер буффера (в пределах 10-100).
        /// </summary>
        public int BufferCoefficient
        {
            get 
            {
                if (Disposed)
                    throw new ObjectDisposedException("Object Disposed");

                return BufferCoefficientValue; 
            }
            set 
            {
                if (Disposed)
                    throw new ObjectDisposedException("Object Disposed");

                BufferCoefficientValue = (value >= 10 && value <= 100) ? value : 20; 
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

            CryptAlgorithm = SymmetricAlg;

            BufferCoefficientValue = 10;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Генерирует ключ и вектор инициализации.
        /// </summary>
        /// <returns>Ключ.</returns>
        public byte[] GenerateKey()
        {
            CryptAlgorithm.GenerateKey();
            CryptAlgorithm.GenerateIV();

            return CryptAlgorithm.Key;
        }

        /// <summary>
        /// Производит шифрование потока.
        /// </summary>
        /// <param name="input_file">Поток, который будет зашифрован.</param>
        /// <param name="output_file">Поток, в который будет записан результат шифрования.</param>
        public void EncryptStream(Stream InputStream, Stream OutputStream)
        {
            if (Disposed)
                throw new ObjectDisposedException("Object Disposed");

            if (InputStream == null || OutputStream == null)
                throw new ArgumentNullException("One of the arguments (or two) equals null");

            ICryptoTransform encryptor = null;
            CryptoStream csEncrypt = null;

            try
            {
                int MaxBufferSizeValue = BufferCoefficientValue * 256 * 1024;

                encryptor = CryptAlgorithm.CreateEncryptor(CryptAlgorithm.Key, CryptAlgorithm.IV);
                csEncrypt = new CryptoStream(OutputStream, encryptor, CryptoStreamMode.Write);

                OutputStream.Write(BitConverter.GetBytes(InputStream.Length), 0, 8);
                OutputStream.Write(CryptAlgorithm.IV, 0, CryptAlgorithm.BlockSize / 8 );
                
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
            finally
            {
                if (encryptor != null)
                    encryptor.Dispose();
            }
        }

        /// <summary>
        /// Производит дешифрование потока.
        /// </summary>
        /// <param name="input_file">Поток, который будет дешифрован.</param>
        /// <param name="output_file">Поток, в который будет записан результат дешифрования.</param>
        public void DecryptStream(Stream InputStream, Stream OutputStream, byte[] Key)
        {
            if (Disposed)
                throw new ObjectDisposedException("Object Disposed");

            if (InputStream == null || OutputStream == null)
                throw new ArgumentNullException("One of the arguments (or two) equals null");

            ICryptoTransform decryptor = null;
            CryptoStream csEncrypt = null;

            try
            {
                int MaxBufferSizeValue = BufferCoefficientValue * 256 * 1024;

                byte[] OriginFileLengthArray = new byte[8];
                byte[] IV = new byte[CryptAlgorithm.BlockSize / 8];

                InputStream.Read(OriginFileLengthArray, 0, 8);
                InputStream.Read(IV, 0, CryptAlgorithm.BlockSize / 8);

                long DeltaLength = InputStream.Length - HeadSize - CryptAlgorithm.BlockSize / 8 - BitConverter.ToInt64(OriginFileLengthArray, 0);

                decryptor = CryptAlgorithm.CreateDecryptor(Key, IV);
                csEncrypt = new CryptoStream(InputStream, decryptor, CryptoStreamMode.Read);

                byte[] DataBuffer = new byte[MaxBufferSizeValue];

                while (InputStream.Position < InputStream.Length)
                {
                    int DataSize = (InputStream.Length - InputStream.Position > MaxBufferSizeValue) ? MaxBufferSizeValue : (int)(InputStream.Length - InputStream.Position - DeltaLength);

                    csEncrypt.Read(DataBuffer, 0, DataSize);
                    OutputStream.Write(DataBuffer, 0, DataSize);
                }
            }
            finally
            {
                if (decryptor != null)
                    decryptor.Dispose();
            }
        }
        #endregion

        #region private methods
        private int CalculateDataSize(int DataSize, int MaxDataSize)
        {
            if (DataSize == MaxDataSize)
                return DataSize;

            int BlockSize = CryptAlgorithm.BlockSize / 8;
            DataSize = (int)Math.Ceiling((double)DataSize / BlockSize) * BlockSize;

            return DataSize;
        }
        #endregion

        #region IDisposable
        private bool Disposed = false;

        public void Dispose()
        {
            if (Disposed == true) return;

            DisposeManagedResources();
        }

        private void DisposeManagedResources()
        {
            CryptAlgorithm.Clear();

            Disposed = true;
        }
        #endregion
    }
}
