using Engine.Api;
using System.Collections.Generic;

namespace Engine.Plugins
{
  public abstract class Plugin<TModel, TCommand> : CrossDomainObject
    where TModel : CrossDomainObject
    where TCommand : CrossDomainObject, ICommand
  {
    public static TModel Model { get; private set; }

    public void Initialize(TModel model)
    {
      Model = model;
      Initialize();
    }

    public abstract string Name { get; }
    public abstract IEnumerable<TCommand> Commands { get; }
    public abstract object NotifierContext { get; }

    protected abstract void Initialize();
  }
}
