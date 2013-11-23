using System;
using System.IO;

namespace TCPChat.Engine
{
    class Logger
    {
        static readonly object syncObj = new object();

        string logFileName;

        public Logger(string FileName)
        {
            logFileName = FileName;
        }

        public void Write(Exception e)
        {
            string Message = String.Format("Time: {0};\r\nType: {1};\r\nMessage: {2};\r\nStackTrace:\r\n{3}\r\n", DateTime.Now.ToString(), e.GetType().ToString() ,e.Message, e.StackTrace);

            lock (syncObj)
            {
                using (FileStream logFile = new FileStream(logFileName, FileMode.Append, FileAccess.Write))
                using (StreamWriter logWriter = new StreamWriter(logFile))
                {
                    logWriter.WriteLine(Message);
                }
            }
        }
    }
}
