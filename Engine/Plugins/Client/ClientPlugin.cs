using Engine.Plugins.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Plugins.Client
{
  /// <summary>
  /// Представляет базовый класс для реализации плагина.
  /// </summary>
  public abstract class ClientPlugin :
    Plugin<ClientModelWrapper>
  {
    public abstract List<ClientPluginCommand> Commands { get; }

    public abstract string MenuCaption { get; }
    public abstract void InvokeMenuHandler();
  }
}
