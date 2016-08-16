using Engine.API;
using Engine.Audio;
using Engine.Helpers;
using Engine.Model.Client;
using Engine.Network;
using System.Security;

namespace Engine.Plugins.Client
{
  public class ClientModelWrapper :
    CrossDomainObject
  {
    public ClientApi Api
    {
      [SecuritySafeCritical]
      get { return ClientModel.Api; }
    }

    public AsyncClient Client
    {
      [SecuritySafeCritical]
      get { return ClientModel.Client; }
    }

    public AsyncPeer Peer
    {
      [SecuritySafeCritical]
      get { return ClientModel.Peer; }
    }

    public IPlayer Player
    {
      [SecuritySafeCritical]
      get { return ClientModel.Player; }
    }

    public IRecorder Recorder
    {
      [SecuritySafeCritical]
      get { return ClientModel.Recorder; }
    }

    public Logger Logger
    {
      [SecuritySafeCritical]
      get { return ClientModel.Logger; }
    }

    /// <summary>
    /// Исользовать только с конструкцией using
    /// </summary>
    /// <example>using (var client = ClientModel.Get()) { ... }</example>
    /// <returns>Возвращает и блокирует модель.</returns>
    [SecuritySafeCritical]
    public ClientGuard Get() { return ClientModel.Get(); }
  }
}
