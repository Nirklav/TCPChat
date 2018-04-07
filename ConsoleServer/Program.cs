using Engine.Helpers;
using Engine.Model.Server;
using Engine.Network;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace ConsoleServer
{
  class Program
  {
    private const string ServerAddressName = "serverAddress";
    private const string ServicePortName = "servicePort";
    private const string CertificatePathName = "certificatePath";
    private const string CertificatePasswordName = "certificatePassword";
    private const string AdminPasswordName = "adminPassword";

    static void Main(string[] argsRaw)
    {
      var args = new ArgsParser(argsRaw, WriteHelp);

      var serverAddresss = args.Get(ServerAddressName);
      var p2pServicePort = args.Get<int>(ServicePortName, int.TryParse);
      var certificatePath = args.Get(CertificatePathName);
      var certificatePassword = args.GetOrDefault(CertificatePasswordName, null);
      var adminPassword = args.Get(AdminPasswordName);
      
      if (!File.Exists(certificatePath))
      {
        Console.WriteLine("File not found: {0}", certificatePath);
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
        AdminPassword = adminPassword,
        PluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"),
        ExcludedPlugins = new string[0],
        Certificate = new X509Certificate2(certificatePath, CreateSecureString(certificatePassword))
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
      Console.WriteLine();
      Console.WriteLine("Parameters required:");
      Console.WriteLine($"\t\"{ServerAddressName}\" \"{ServicePortName}\" \"{CertificatePathName}\" \"{CertificatePasswordName}\" \"{AdminPasswordName}\"");
      Console.WriteLine("Example:");
      Console.WriteLine($"\t{ServerAddressName} 127.0.0.1:10021 {ServicePortName} 10022 {CertificatePathName} server.pfx {CertificatePasswordName} 1111 {AdminPasswordName} 1112");
    }

    private static unsafe SecureString CreateSecureString(string password)
    {
      if (password != null)
        fixed (char* passwordPtr = password)
          return new SecureString(passwordPtr, password.Length);
      return null;
    }
  }
}
