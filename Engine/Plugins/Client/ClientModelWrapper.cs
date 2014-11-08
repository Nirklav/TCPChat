using Engine.Audio;
using Engine.Helpers;
using Engine.Model.Client;
using Engine.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Plugins.Client
{
  public class ClientModelWrapper : 
    CrossDomainObject
  {
    public ClientAPIWrapper API { get; private set; }
    public AsyncClient Client { get; private set; }
    public AsyncPeer Peer { get; private set; }
    public IPlayer Player { get; private set; }
    public IRecorder Recorder { get; private set; }
    public Logger Logger { get; private set; }

    /// <summary>
    /// Исользовать только с конструкцией using
    /// </summary>
    /// <example>using (var client = ClientModel.Get()) { ... }</example>
    /// <returns>Возвращает и блокирует модель.</returns>
    public ClientContext Get() { return ClientModel.Get(); }

    public ClientModelWrapper()
    {
      API = new ClientAPIWrapper();
      Client = ClientModel.Client;
      Peer = ClientModel.Peer;
      Player = ClientModel.Player;
      Recorder = ClientModel.Recorder;
      Logger = ClientModel.Logger;
    }
  }
}
