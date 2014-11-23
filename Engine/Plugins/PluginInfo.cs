using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Plugins
{
  [Serializable]
  class PluginInfo
  {
    public bool Loaded { get; set; }
    public string Name { get; set; }
    public string AssemblyPath { get; private set;}
    public string TypeName { get; private set; }

    public PluginInfo(string pluginAssemblyPath, string pluginTypeName)
    {
      AssemblyPath = pluginAssemblyPath;
      TypeName = pluginTypeName;
    }
  }
}
