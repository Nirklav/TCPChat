using Engine.Exceptions;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.Plugins.Server
{
  [SecurityCritical]
  public class ServerPluginManager :
    PluginManager<ServerPlugin, ServerModelWrapper, ServerPluginCommand>
  {
    [SecurityCritical]
    public ServerPluginManager(string path)
      : base(path)
    {

    }

    [SecurityCritical]
    protected override void OnError(string message, Exception e)
    {
      ServerModel.Logger.Write(new ModelException(ErrorCode.PluginError, string.Format("Error: {0}", message), e));
    }
  }
}
