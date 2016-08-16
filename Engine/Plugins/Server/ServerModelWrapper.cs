using Engine.API;
using Engine.Helpers;
using Engine.Model.Server;
using Engine.Network;
using System.Security;

namespace Engine.Plugins.Server
{
  public class ServerModelWrapper :
    CrossDomainObject
  {
    public ServerApi Api
    {
      [SecuritySafeCritical]
      get { return ServerModel.Api; }
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
    public ServerGuard Get()
    {
      return ServerModel.Get();
    }
  }
}
