using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

namespace Engine.Plugins
{
  class PluginInfoLoader
  {
    private class Proxy : MarshalByRefObject
    {
      public string[] PluginLibs { get; set; }
      public string FullTypeName { get; set; }

      public List<PluginInfo> PluginInfos { get; set; }

      public void LoadInfos()
      {
        foreach (var assemblyPath in PluginLibs)
        {
          var assembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(assemblyPath).FullName);
          foreach (var type in assembly.GetExportedTypes())
          {
            if (type.IsAbstract)
              continue;

            var currentBaseType = type.BaseType;
            while (currentBaseType != typeof(object))
            {
              if (string.Compare(currentBaseType.FullName, FullTypeName, StringComparison.OrdinalIgnoreCase) == 0)
              {
                PluginInfos.Add(new PluginInfo(assemblyPath, type.FullName));
                break;
              }
              currentBaseType = currentBaseType.BaseType;
            }
          }
        }
      }
    }

    public List<PluginInfo> LoadFrom(string typeName, string[] inputPluginLibs)
    {
      if (inputPluginLibs.Length <= 0)
        return null;

      var domainSetup = new AppDomainSetup();
      domainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
      domainSetup.PrivateBinPath = "plugins;bin";

      var permmisions = new PermissionSet(PermissionState.None);
      permmisions.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
      permmisions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
      permmisions.AddPermission(new UIPermission(UIPermissionWindow.AllWindows));
      permmisions.AddPermission(new FileIOPermission(FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read, inputPluginLibs));

      List<PluginInfo> result;

      var proxyType = typeof(Proxy);
      var engineStrongName = proxyType.Assembly.Evidence.GetHostEvidence<StrongName>();
      if (engineStrongName == null)
        return null;

      var pluginLoader = AppDomain.CreateDomain("Plugin loader", null, domainSetup, permmisions, engineStrongName);
      try
      {
        var proxy = (Proxy)pluginLoader.CreateInstanceAndUnwrap(proxyType.Assembly.FullName, proxyType.FullName);

        proxy.PluginInfos = new List<PluginInfo>();
        proxy.PluginLibs = inputPluginLibs;
        proxy.FullTypeName = typeName;
        proxy.LoadInfos();

        result = proxy.PluginInfos;
      }
      finally
      {
        AppDomain.Unload(pluginLoader);
      }

      return result;
    }
  }
}
