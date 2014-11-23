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
    public ClientPlugin Plugin { get; private set; }

    public PluginEventArgs(ClientPlugin plugin)
    {
      Plugin = plugin;
    }
  }
}
