using Engine.Helpers;
using Engine.Model.Server;
using Engine.Network;
using System;

namespace Engine.Plugins.Server
{
  public class ServerModelWrapper :
    MarshalByRefObject,
    IPluginModelWrapper
  {
    public ServerAPIWrapper API { get; private set; }
    public AsyncServer Server { get; private set; }
    public Logger Logger { get; private set; }

    /// <summary>
    /// Исользовать только с конструкцией using
    /// </summary>
    /// <example>using (var server = SeeverModel.Get()) { ... }</example>
    /// <returns>Возвращает и блокирует модель.</returns>
    public ServerContext Get() { return ServerModel.Get(); }

    public ServerModelWrapper()
    {
      API = new ServerAPIWrapper();
      Server = ServerModel.Server;
      Logger = ServerModel.Logger;
    }
  }
}
