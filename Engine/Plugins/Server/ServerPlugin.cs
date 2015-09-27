using System.Collections.Generic;

namespace Engine.Plugins.Server
{
  /// <summary>
  /// Представляет базовый класс для реализации серверного плагина.
  /// </summary>
  public abstract class ServerPlugin :
    Plugin<ServerModelWrapper, ServerPluginCommand>
  {

  }
}
