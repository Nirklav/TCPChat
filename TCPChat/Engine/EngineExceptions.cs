using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCPChat.Engine
{
    /// <summary>
    /// Представляет ошибку, которая возникает если API не поддерживается.
    /// </summary>
    class APINotSupprtedException : Exception
    {
        const string message = "Приложение не поддерживает эту версию API. Соединение разорвано.";

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="serverAPI">Версия API которая не поддерживается.</param>
        public APINotSupprtedException(string serverAPI) 
            : base(message)
        {
            ServerAPIVersion = serverAPI;
        }

        /// <summary>
        /// Версия которую использует сервер.
        /// </summary>
        public string ServerAPIVersion { get; private set; }
    }

    /// <summary>
    /// Представляет ошибку, которая возникает если свободный порт не найден.
    /// </summary>
    class FreePortDontFindException : Exception
    {
        const string message = "Свободный порт не найден.";

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        public FreePortDontFindException()
            : base(message)
        {

        }
    }

    /// <summary>
    /// Представляет ошибку, которая возникает если файл уже загружается.
    /// </summary>
    class FileAlreadyDownloadingException : Exception
    {
        const string message = "Файл уже загружается.";

        /// <summary>
        /// Описание файла который уже загружается.
        /// </summary>
        public FileDescription File { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="file">Файл который уже существует.</param>
        public FileAlreadyDownloadingException(FileDescription file)
            : base(message)
        {
            File = file;
        }
    }

    /// <summary>
    /// Представляет ошибку получения данных.
    /// </summary>
    class ReceivedDataException : Exception
    {
        const string message = "Получаемые данные имеют больший размер, чем максимально утновленное значение.";

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        public ReceivedDataException()
            : base(message)
        {

        }
    }
}
