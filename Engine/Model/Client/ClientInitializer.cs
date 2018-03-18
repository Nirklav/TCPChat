using System.Drawing;
using System.Security.Cryptography.X509Certificates;

namespace Engine.Model.Client
{
  public class ClientInitializer
  {
    public string Nick { get; set; }
    public Color NickColor { get; set; }
    public X509Certificate2 Certificate { get; set; }

    public string PluginsPath { get; set; }
    public string[] ExcludedPlugins { get; set; }
  }
}
