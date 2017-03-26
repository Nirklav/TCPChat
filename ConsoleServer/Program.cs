using Engine.Model.Server;
using System;
using System.IO;

namespace ConsoleServer
{
  class Program
  {
    static void Main(string[] args)
    {
      string password;
      int serverPort;
      int servicePort;
      bool usingIpV6;

      if (args.Length < 4)
      {
        WriteHelp();
        return;
      }

      password = args[0];

      if (!int.TryParse(args[1], out serverPort))
      {
        WriteHelp();
        return;
      }

      if (!int.TryParse(args[2], out servicePort))
      {
        WriteHelp();
        return;
      }

      if (!bool.TryParse(args[3], out usingIpV6))
      {
        WriteHelp();
        return;
      }

      var initializer = new ServerInitializer
      {
        AdminPassword = password,
        PluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"),
        ExcludedPlugins = new string[0]
      };

      ServerModel.Init(initializer);
      ServerModel.Server.Start(serverPort, servicePort, usingIpV6);

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
      Console.WriteLine("Parameters: \"password\" \"serverPort\" \"servicePort\" \"usingIpV6\"");
    }
  }
}
