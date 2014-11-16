using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Plugins.Server
{
  public abstract class ServerPluginCommand :
    CrossDomainObject,
    ICommand<ServerCommandArgs>
  {
    public abstract ushort Id { get; }
    public abstract void Run(ServerCommandArgs args);
  }
}
