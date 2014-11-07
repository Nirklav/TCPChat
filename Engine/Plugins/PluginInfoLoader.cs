using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Engine.Plugins
{
  [Serializable]
  struct PluginInfo
  {
    private string assemblyPath;
    private string typeName;

    public PluginInfo(string assemblyPath, string typeName)
    {
      this.assemblyPath = assemblyPath;
      this.typeName = typeName;
    }

    public string AssemblyPath { get { return assemblyPath; } }
    public string TypeName { get { return typeName; } }
  }

  class PluginInfoLoader : MarshalByRefObject
  {
    private string[] pluginLibs;
    private List<PluginInfo> pluginInfos;
    private string fullTypeName;

    public List<PluginInfo> LoadFrom(string typeName, string[] inputPluginLibs)
    {
      // init cross domain variables
      pluginInfos = new List<PluginInfo>();
      pluginLibs = inputPluginLibs;
      fullTypeName = typeName;

      var pluginLoader = AppDomain.CreateDomain("Plugin loader"); //TODO: create sandbox
      pluginLoader.DoCallBack(LoadInfos);

      AppDomain.Unload(pluginLoader);

      return pluginInfos;
    }

    private void LoadInfos()
    {
      foreach (var assemblyPath in pluginLibs)
      {
        try
        {
          var assembly = Assembly.LoadFile(assemblyPath);
          foreach (var type in assembly.GetExportedTypes())
          {
            var currentBaseType = type.BaseType;
            while (currentBaseType != typeof(object))
            {
              if (string.Compare(currentBaseType.FullName, fullTypeName, StringComparison.OrdinalIgnoreCase) == 0)
              {
                pluginInfos.Add(new PluginInfo(assemblyPath, type.FullName));
                break;
              }
              currentBaseType = currentBaseType.BaseType;
            }
          }
        }
        catch(Exception e)
        {
          //TODO: log in plugin logger
        }
      }
    }
  }
}
