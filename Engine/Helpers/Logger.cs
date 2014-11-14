using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Engine.Helpers
{
  public class Logger : MarshalByRefObject
  {
    private const string DebugMessageTemplate = "Time: {0} DebugMessage: {1}";
    private const string WarningTemplate = "Time: {0} WARNING: {1};{3}StackTrace:{3}{2}{3}";
    private const string InfoTemplate = "Time: {0} INFO: {1}";
    private const string MessageTemplate = "{4}Time: {0};{5}{4}Type: {1};{5}{4}Message: {2};{5}{4}StackTrace:{5}{3}{5}";
    private const string InnerTemplate = "{1}InnerException: {2}{2}{0}{2}";

    private static readonly object syncObj = new object();

    string logFileName;

    public Logger(string FileName)
    {
      logFileName = FileName;
    }

    public void WriteInfo(string message, params object[] param)
    {
      Write(string.Format(InfoTemplate, DateTime.Now, message), param);
    }

    public void Write(Exception e)
    {
      string message = CreateLogMessage(e, 0);

      Write(message);
    }

    public void WriteDebug(string message, params object[] args)
    {
#if DEBUG
      Write(string.Format(DebugMessageTemplate, DateTime.Now, message), args);
#endif
    }

    public void WriteWarning(string message, params object[] args)
    {
      StackTrace stackTrace = new StackTrace(true);
      Write(string.Format(WarningTemplate, DateTime.Now, message, stackTrace, Environment.NewLine), args);
    }

    private void Write(string message, params object[] args)
    {
      lock (syncObj)
      {
        using (var logFile = new FileStream(logFileName, FileMode.Append, FileAccess.Write))
        {
          StreamWriter logWriter = new StreamWriter(logFile);
          logWriter.WriteLine(string.Format(message, args));
        }
      }
    }

    private string CreateLogMessage(Exception e, int level)
    {
      StringBuilder tabs = new StringBuilder();
      for (int i = 0; i < level; i++)
        tabs.Append("  ");

      StringBuilder stackTrace = new StringBuilder(e.StackTrace);
      stackTrace.Replace("  ", "  " + tabs);

      StringBuilder builder = new StringBuilder(200);
      builder.AppendFormat(MessageTemplate, 
        DateTime.Now, 
        e.GetType(), 
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
