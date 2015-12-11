using Engine.API;

namespace Engine.Plugins.Client
{
  public abstract class ClientPluginCommand :
    CrossDomainObject,
    ICommand<ClientCommandArgs>
  {
    public abstract long Id { get; }
    public abstract void Run(ClientCommandArgs args);
  }
}
