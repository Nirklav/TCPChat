using Engine.Plugins.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
