using System;
using System.IO;
using System.Text;

namespace Engine.Concrete.Helpers
{
  public class Logger
  {
    private static readonly object syncObj = new object();
    private const string MessageTemplate = "{4}Time: {0};{5}{4}Type: {1};{5}{4}Message: {2};{5}{4}StackTrace:{5}{3}{5}";
    private const string InnerTemplate = "{1}InnerException: {2}{2}{0}{2}";

    string logFileName;

    public Logger(string FileName)
    {
      logFileName = FileName;
    }

    public void Write(Exception e)
    {
      string Message = CreateLogMessage(e, 0);

      lock (syncObj)
      {
        using (FileStream logFile = new FileStream(logFileName, FileMode.Append, FileAccess.Write))
        using (StreamWriter logWriter = new StreamWriter(logFile))
        {
          logWriter.WriteLine(Message);
        }
      }
    }

    private string CreateLogMessage(Exception e, int level)
    {
      StringBuilder tabs = new StringBuilder();
      for (int i = 0; i < level; i++)
        tabs.Append("  ");

      StringBuilder stackTrace = new StringBuilder(e.StackTrace);
      stackTrace.Replace("  ", "  " + tabs.ToString());

      StringBuilder builder = new StringBuilder(200);
      builder.AppendFormat(MessageTemplate, 
        DateTime.Now.ToString(), 
        e.GetType().ToString(), 
        e.Message,
        stackTrace, 
        tabs, 
        Environment.NewLine);

      if (e.InnerException != null)
        builder.AppendFormat(InnerTemplate, CreateLogMessage(e.InnerException, level + 1), tabs, Environment.NewLine);

      return builder.ToString();
    }
  }
}
