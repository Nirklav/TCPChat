using Engine.API;
using Engine.API.ClientCommands;
using Engine.Exceptions;
using Engine.Helpers;
using Engine.Network.Connections;

namespace Engine.Plugins.Client
{
  public abstract class ClientPluginCommand : ClientCommand 
  {
  }

  public abstract class ClientPluginCommand<TContent> : ClientPluginCommand
  {
    protected sealed override void OnRun(ClientCommandArgs args)
    {
      var package = args.Unpacked.Package as IPackage<byte[]>;
      if (package == null)
        throw new ModelException(ErrorCode.WrongContentType);

      var content = Serializer.Deserialize<TContent>(package.Content);
      OnRun(content, args);
    }

    protected abstract void OnRun(TContent content, ClientCommandArgs args);
  }
}
