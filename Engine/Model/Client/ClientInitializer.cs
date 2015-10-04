using System.Drawing;

namespace Engine.Model.Client
{
  public class ClientInitializer
  {
    public string Nick { get; set; }
    public Color NickColor { get; set; }

    public string PluginsPath { get; set; }
    public string[] ExcludedPlugins { get; set; }
  }
}
