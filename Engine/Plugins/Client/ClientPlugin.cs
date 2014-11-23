using Engine.Model.Client;
using System.Collections.Generic;

namespace Engine.Plugins.Client
{
  /// <summary>
  /// Представляет базовый класс для реализации плагина.
  /// </summary>
  public abstract class ClientPlugin :
    Plugin<ClientModelWrapper>
  {
    public virtual ClientNotifierContext NotifierContext { get { return null; } }

    public abstract List<ClientPluginCommand> Commands { get; }
    public abstract string MenuCaption { get; }
    public abstract void InvokeMenuHandler();
  }
}
