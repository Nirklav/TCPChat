using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
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
      var domainSetup = new AppDomainSetup();
      domainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
      domainSetup.PrivateBinPath = "plugins;bin";

      var permmisions = new PermissionSet(PermissionState.None);
      permmisions.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
      permmisions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
      permmisions.AddPermission(new FileIOPermission(FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read, inputPluginLibs));

      var pluginLoader = AppDomain.CreateDomain("Plugin loader", null, domainSetup, permmisions);
      try
      {
        pluginInfos = new List<PluginInfo>();
        pluginLibs = inputPluginLibs;
        fullTypeName = typeName;

        pluginLoader.DoCallBack(LoadInfos);
      }
      finally
      {
        AppDomain.Unload(pluginLoader);
      }

      return pluginInfos;
    }

    private void LoadInfos()
    {
      foreach (var assemblyPath in pluginLibs)
      {
        var assembly = Assembly.LoadFile(assemblyPath);
        foreach (var type in assembly.GetExportedTypes())
        {
          if (type.IsAbstract)
            continue;

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
    }
  }
}
