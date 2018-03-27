using Engine.Helpers;
using Engine.Model.Server;
using Engine.Network;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace ConsoleServer
{
  class Program
  {
    static void Main(string[] args)
    {
      if (args.Length < 4)
      {
        WriteHelp();
        return;
      }
      
      var serverAddresss = args[0];

      if (!int.TryParse(args[1], out var p2pServicePort))
      {
        WriteHelp();
        return;
      }

      var certificatePath = args[2];
      if (File.Exists(certificatePath))
      {
        Console.WriteLine("File not found: {0}", certificatePath);
        WriteHelp();
        return;
      }
      
      var password = args[3];

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
        ExcludedPlugins = new string[0],
        Certificate = new X509Certificate2(certificatePath)
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
      Console.WriteLine("Parameters: \"serverAddress\" \"servicePort\" \"certificatePath\" \"password\"");
    }
  }
}
