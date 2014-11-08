using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Plugins
{
  public abstract class Plugin<TModel> : CrossDomainObject
  {
    public abstract void Initialize(TModel model);
    public abstract string Name { get; }
  }
}
