using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace Engine.Helpers
{
  public class Logger : MarshalByRefObject
  {
    private const string DebugMessageTemplate = "Time: {0} DEBUG: {1}";
    private const string WarningTemplate = "Time: {0} WARNING: {1};{3}StackTrace:{3}{2}{3}";
    private const string InfoTemplate = "Time: {0} INFO: {1}";
    private const string MessageTemplate = "{4}Time: {0};{5}{4}Type: {1};{5}{4}Message: {2};{5}{4}StackTrace:{5}{3}{5}";
    private const string InnerTemplate = "{1}InnerException: {2}{2}{0}{2}";

    private const int WriteAttempts = 5;

    private static readonly Dictionary<string, object> syncObjects = new Dictionary<string, object>();
    private static object GetSyncObject(string fileName)
    {
      lock (syncObjects)
      {
        object syncObject;
        syncObjects.TryGetValue(fileName, out syncObject);
        return syncObject;
      }
    }

    private string logFileName;
    
    [SecurityCritical]
    public Logger(string fileName)
    {
      logFileName = fileName;
      syncObjects.Add(logFileName, new object());
    }

    [SecuritySafeCritical]
    public void WriteInfo(string message, params object[] param)
    {
      Write(string.Format(InfoTemplate, DateTime.Now, message), param);
    }

    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    public void WriteDebug(string message, params object[] args)
    {
      Write(string.Format(DebugMessageTemplate, DateTime.Now, message), args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SecuritySafeCritical]
    public void WriteWarning(string message, params object[] args)
    {
      StackTrace stackTrace = new StackTrace(1, true);
      Write(string.Format(WarningTemplate, DateTime.Now, message, stackTrace, Environment.NewLine), args);
    }

    [SecuritySafeCritical]
    public void Write(Exception e)
    {
      string message = CreateLogMessage(e, 0);
      Write(message);
    }

    [SecurityCritical]
    [FileIOPermission(SecurityAction.Assert, AllLocalFiles = FileIOPermissionAccess.Append)]
    private void Write(string message, params object[] args)
    {
      lock (GetSyncObject(logFileName))
      {
        int attempts = 0;
        while (true)
        {
          FileStream logFile = null;
          StreamWriter logWriter = null;
          try
          {
            logFile = new FileStream(logFileName, FileMode.Append, FileAccess.Write);
            logWriter = new StreamWriter(logFile);
          
            logWriter.WriteLine(string.Format(message, args));
          }
          catch (IOException)
          {
            attempts++;
            if (attempts >= WriteAttempts)
              return;

            continue;
          }
          finally
          {
            if (logWriter != null)
              logWriter.Dispose();

            if (logFile != null)
              logFile.Dispose();
          }

          break;
        }
      }
    }

    [SecurityCritical]
    private string CreateLogMessage(Exception e, int level)
    {
      var tabs = new StringBuilder();
      for (int i = 0; i < level; i++)
        tabs.Append("  ");

      var stackTrace = new StringBuilder(e.StackTrace);
      stackTrace.Replace("  ", "  " + tabs);

      var builder = new StringBuilder(200);
      builder.AppendFormat(MessageTemplate, DateTime.Now, e.GetType(), e.Message, stackTrace, tabs, Environment.NewLine);

      if (e.InnerException != null)
        builder.AppendFormat(InnerTemplate, CreateLogMessage(e.InnerException, level + 1), tabs, Environment.NewLine);

      return builder.ToString();
    }
  }
}
