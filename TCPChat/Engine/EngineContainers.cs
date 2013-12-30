using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TCPChat.Engine.Connections;

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

        /// <summary>
        /// Полное имя файла.
        /// </summary>
        public string FullName { get; set; }

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
    public class WaitingPrivateMessage
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

    /// <summary>
    /// Команды ожидающие прямого подключения к клиенту.
    /// </summary>
    public class WaitingCommand
    {
        /// <summary>
        /// Создает экземпляр класса.
        /// </summary>
        /// <param name="info">Пользователь которому необходимо послать команду.</param>
        /// <param name="id">Индетификатор команды.</param>
        /// <param name="content">Параметр команды.</param>
        public WaitingCommand(UserDescription info, ushort id, object content)
        {
            Info = info;
            CommandId = id;
            MessageContent = content;
        }

        /// <summary>
        /// Пользователь которому необходимо послать команду, после подключения.
        /// </summary>
        public UserDescription Info { get; set; }

        /// <summary>
        /// Индетификатор команды.
        /// </summary>
        public ushort CommandId { get; set; }

        /// <summary>
        /// Параметр команды.
        /// </summary>
        public object MessageContent { get; set; }
    }

    /// <summary>
    /// Класс хранящий соединения.
    /// </summary>
    public class ConnectionsContainer
    {
        /// <summary>
        /// Создает класс хранящий соединения.
        /// </summary>
        /// <param name="requestConnection">Соединение которое получит ответ.</param>
        /// <param name="sendedConnection">Соединение которое прислало запрос.</param>
        public ConnectionsContainer(ServerConnection requestConnection, ServerConnection senderConnection)
        {
            RequestConnection = requestConnection;
            SenderConnection = senderConnection;
        }

        /// <summary>
        /// Соединение у которого запрашивают прямое соединение.
        /// </summary>
        public ServerConnection RequestConnection { get; set; }

        /// <summary>
        /// Соединение запрашивающее прямое соединение.
        /// </summary>
        public ServerConnection SenderConnection { get; set; }

        /// <summary>
        /// Точка принимающая прямое соединение. У пользователя к которому хотят подключится.
        /// </summary>
        public IPEndPoint RequestPeerPoint { get; set; }

        /// <summary>
        /// Точка принимающая прямое соединение. У пользователя который запрашивает подключение.
        /// </summary>
        public IPEndPoint SenderPeerPoint { get; set; }
    }
}
