using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Plugins
{
  public interface IPluginModelWrapper
  {

  }

  public abstract class Plugin<TModel> : Plugin
    where TModel : IPluginModelWrapper
  {
    public abstract void Initialize(TModel model);
  }

  public abstract class Plugin : CrossDomainObject
  {
    public abstract string Name { get; }
  }
}
