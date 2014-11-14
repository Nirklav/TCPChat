using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UI.Infrastructure
{
  public class PluginSetting
  {
    public PluginSetting() { }

    public PluginSetting(string name)
    {
      Name = name;
      Enabled = true;
    }

    public string Name { get; set; }
    public bool Enabled { get; set; }
  }
}
