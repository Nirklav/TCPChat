using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TCPChat.Engine
{
    /// <summary>
    /// Класс описывающий загружаемый файл.
    /// </summary>
    public class DownloadingFile : IDisposable
    {
        /// <summary>
        /// Описание файла.
        /// </summary>
        public FileDescription File { get; set; }

        /// <summary>
        /// Поток для сохранения файла.
        /// </summary>
        public FileStream WriteStream { get; set; }

        bool disposed = false;

        /// <summary>
        /// Освобождает ресурсы.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            if (WriteStream != null)
                WriteStream.Dispose();
        }
    }

    /// <summary>
    /// Класс описывающий выложенный файл.
    /// </summary>
    public class PostedFile : IDisposable
    {
        /// <summary>
        /// Описание файла.
        /// </summary>
        public FileDescription File { get; set; }

        /// <summary>
        /// Комната в которую выложен файл.
        /// </summary>
        public string RoomName { get; set; }

        /// <summary>
        /// Поток для чтения файла.
        /// </summary>
        public FileStream ReadStream { get; set; }

        bool disposed = false;

        /// <summary>
        /// Освобождает ресурсы.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            if (ReadStream != null)
                ReadStream.Dispose();
        }
    }

    /// <summary>
    /// Класс описывающий ожидающее отркытого ключа приватное сообщение.
    /// </summary>
    public class AwaitingPrivateMessage
    {
        /// <summary>
        /// Получатель сообщения.
        /// </summary>
        public string Receiver { get; set; }

        /// <summary>
        /// Сообщение.
        /// </summary>
        public string Message { get; set; }
    }
}
