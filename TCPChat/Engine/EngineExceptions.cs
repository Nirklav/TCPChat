using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCPChat.Engine
{
    class APINotSupprtedException : Exception
    {
        const string message = "Приложение не поддерживает эту версию API. Соединение разорвано.";

        public APINotSupprtedException(string serverAPI) 
            : base(message)
        {
            ServerAPIVersion = serverAPI;
        }

        public string ServerAPIVersion { get; private set; }
    }

    class ReceivedDataException : Exception
    {
        const string message = "Получаемые данные имеют больший размер, чем максимально утновленное значение.";

        public ReceivedDataException()
            : base(message)
        {

        }
    }
}
