namespace Engine.Model.Server
{
  public class ServerInitializer
  {
    public string AdminPassword { get; set; }
    public string PluginsPath { get; set; }
    public string[] ExcludedPlugins { get; set; }
  }
}
