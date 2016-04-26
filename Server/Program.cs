using Engine.Model.Server;
using System;
using System.IO;

namespace Server
{
  class Program
  {
    static void Main(string[] args)
    {
      int serverPort;
      int servicePort;
      bool usingIpV6;

      if (args.Length < 3)
      {
        Console.WriteLine("Parameters: \"serverPort\" \"servicePort\" \"usingIpV6\"");
        return;
      }

      if (!int.TryParse(args[0], out serverPort))
      {
        Console.WriteLine("Parameters: \"serverPort\" \"servicePort\" \"usingIpV6\"");
        return;
      }

      if (!int.TryParse(args[1], out servicePort))
      {
        Console.WriteLine("Parameters: \"serverPort\" \"servicePort\" \"usingIpV6\"");
        return;
      }

      if (!bool.TryParse(args[2], out usingIpV6))
      {
        Console.WriteLine("Parameters: \"serverPort\" \"servicePort\" \"usingIpV6\"");
        return;
      }

      var initializer = new ServerInitializer
      {
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
  }
}
