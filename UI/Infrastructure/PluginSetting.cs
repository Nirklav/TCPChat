using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UI.Infrastructure
{
  public class PluginSetting
  {
    public PluginSetting() { }

    public PluginSetting(string name, bool enabled)
    {
      Name = name;
      Enabled = enabled;
    }

    public string Name { get; set; }
    public bool Enabled { get; set; }
  }
}
