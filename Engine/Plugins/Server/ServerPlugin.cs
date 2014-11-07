using Engine.Plugins.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
