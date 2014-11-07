using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Plugins
{
  public interface IPlugin
  {
    string Name { get; }

    void Initialize();
  }
}
