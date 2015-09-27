using Engine.Helpers;
using Engine.Model.Server;
using Engine.Network;
using System.Security;

namespace Engine.Plugins.Server
{
  [SecuritySafeCritical]
  public class ServerModelWrapper :
    CrossDomainObject
  {
    public ServerAPIWrapper API
    {
      [SecuritySafeCritical]
      get;
      [SecuritySafeCritical]
      private set;
    }

    public AsyncServer Server
    {
      [SecuritySafeCritical]
      get { return ServerModel.Server; }
    }

    public Logger Logger
    {
      [SecuritySafeCritical]
      get { return ServerModel.Logger; }
    }

    /// <summary>
    /// Исользовать только с конструкцией using
    /// </summary>
    /// <example>using (var server = SeeverModel.Get()) { ... }</example>
    /// <returns>Возвращает и блокирует модель.</returns>
    [SecuritySafeCritical]
    public ServerContext Get()
    {
      return ServerModel.Get();
    }

    [SecuritySafeCritical]
    public ServerModelWrapper()
    {
      API = new ServerAPIWrapper();
    }
  }
}
