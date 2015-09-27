using System;

namespace Engine.Plugins
{
  [Serializable]
  class PluginInfo
  {
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
