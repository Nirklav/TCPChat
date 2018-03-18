using Engine.Helpers;
using Engine.Model.Server;
using Engine.Network;
using System;
using System.IO;

namespace ConsoleServer
{
  class Program
  {
    static void Main(string[] args)
    {
      if (args.Length < 3)
      {
        WriteHelp();
        return;
      }

      var password = args[0];
      var serverAddresss = args[1];
      if (!int.TryParse(args[2], out var p2pServicePort))
      {
        WriteHelp();
        return;
      }

      AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
      {
        var error = e.ExceptionObject as Exception;
        if (error == null)
          return;

        var logger = new Logger(AppDomain.CurrentDomain.BaseDirectory + "/UnhandledError.log");
        logger.Write(error);
      };

      var initializer = new ServerInitializer
      {
        AdminPassword = password,
        PluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"),
        ExcludedPlugins = new string[0]
      };

      ServerModel.Init(initializer);
      var serverUri = Connection.CreateTcpchatUri(serverAddresss);
      ServerModel.Server.Start(serverUri, p2pServicePort);

      Console.WriteLine("Enter \"exit\" for server stop");

      while (true)
      {
        var command = Console.ReadLine();
        if (string.Equals(command, "exit", StringComparison.OrdinalIgnoreCase))
          break;
      }

      ServerModel.Reset();
    }

    private static void WriteHelp()
    {
      Console.WriteLine("Parameters: \"password\" \"serverAddress\" \"servicePort\"");
    }
  }
}
