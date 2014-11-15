using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Model.Server
{
  public class ServerInitializer
  {
    public IServerAPI API { get; set; }

    public string PluginsPath { get; set; }
    public string[] ExcludedPlugins { get; set; }
  }
}
