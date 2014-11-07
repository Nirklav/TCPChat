using System.Collections.Generic;

namespace Engine.Plugins.Server
{
  /// <summary>
  /// Представляет базовый класс для реализации плагина.
  /// </summary>
  public abstract class ServerPlugin :
    Plugin<ServerModelWrapper>
  {
    public abstract List<ServerPluginCommand> Commands { get; }
  }
}
