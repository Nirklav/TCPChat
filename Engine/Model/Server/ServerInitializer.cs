using System.Security.Cryptography.X509Certificates;

namespace Engine.Model.Server
{
  public class ServerInitializer
  {
    public string AdminPassword { get; set; }
    public string PluginsPath { get; set; }
    public string[] ExcludedPlugins { get; set; }
    public X509Certificate2 Certificate { get; set; }
  }
}
