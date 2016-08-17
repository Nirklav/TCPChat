using Engine.API;
using Engine.API.ServerCommands;
using Engine.Exceptions;
using Engine.Helpers;
using Engine.Network.Connections;

namespace Engine.Plugins.Server
{
  public abstract class ServerPluginCommand : ServerCommand
  {
  }

  public abstract class ServerPluginCommand<TContent> : ServerPluginCommand
  {
    protected sealed override void OnRun(ServerCommandArgs args)
    {
      var package = args.Unpacked.Package as IPackage<byte[]>;
      if (package == null)
        throw new ModelException(ErrorCode.WrongContentType);

      var content = Serializer.Deserialize<TContent>(package.Content);
      OnRun(content, args);
    }

    protected abstract void OnRun(TContent content, ServerCommandArgs args);
  }
}
