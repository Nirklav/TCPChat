using System;

namespace Engine
{
  [Serializable]
  public class PluginEventArgs : EventArgs
  {
    public string PluginName { get; private set; }

    public PluginEventArgs(string pluginName)
    {
      PluginName = pluginName;
    }
  }
}
