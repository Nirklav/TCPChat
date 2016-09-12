using Engine.Api.Client;
using Engine.Audio;
using Engine.Audio.OpenAL;
using Engine.Helpers;
using Engine.Model.Client.Entities;
using Engine.Model.Common;
using Engine.Network;
using Engine.Plugins.Client;
using System;
using System.Security;
using System.Threading;

namespace Engine.Model.Client
{
  [SecurityCritical]
  public static class ClientModel
  {
    private static ClientChat _chat;
    private static Logger _logger = new Logger("Client.log");
    private static IPlayer _player = new OpenALPlayer();
    private static IRecorder _recorder = new OpenALRecorder();
    private static IClientNotifier _notifier = NotifierGenerator.MakeInvoker<IClientNotifier>();

    /// <summary>
    /// Logger.
    /// </summary>
    public static Logger Logger
    {
      [SecurityCritical]
      get { return _logger; }
    }

    /// <summary>
    /// Client api.
    /// </summary>
    public static ClientApi Api
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Client.
    /// </summary>
    public static AsyncClient Client
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Peer.
    /// </summary>
    public static AsyncPeer Peer
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Sound player.
    /// </summary>
    public static IPlayer Player
    {
      [SecurityCritical]
      get { return _player; }
    }

    /// <summary>
    /// Sound recorder.
    /// </summary>
    public static IRecorder Recorder
    {
      [SecurityCritical]
      get { return _recorder; }
    }

    /// <summary>
    /// Plugins manager.
    /// </summary>
    public static ClientPluginManager Plugins
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Notifier.
    /// </summary>
    public static IClientNotifier Notifier
    {
      [SecurityCritical]
      get { return _notifier; }
    }

    /// <summary>
    /// Creates and returns guard that lock chat data.
    /// </summary>
    /// <example>using (var client = ClientModel.Get()) { ... }</example>
    /// <returns>Client guard.</returns>
    [SecurityCritical]
    public static ClientGuard Get()
    {
      if (Interlocked.CompareExchange(ref _chat, null, null) == null)
        throw new ArgumentException("model do not inited yet");

      return new ClientGuard(_chat);
    }

    /// <summary>
    /// Returns true if model intitialized, otherwise false.
    /// </summary>
    public static bool IsInited
    {
      [SecurityCritical]
      get { return Interlocked.CompareExchange(ref _chat, null, null) != null; }
    }

    /// <summary>
    /// Initialize model.
    /// </summary>
    /// <param name="initializer">Client initializer.</param>
    [SecurityCritical]
    public static void Init(ClientInitializer initializer)
    {
      var user = new ClientUser(initializer.Nick, initializer.NickColor);

      if (Interlocked.CompareExchange(ref _chat, new ClientChat(user), null) != null)
        throw new InvalidOperationException("model already inited");

      Api = new ClientApi();
      Client = new AsyncClient(initializer.Nick);
      Peer = new AsyncPeer();

      Plugins = new ClientPluginManager(initializer.PluginsPath);
      Plugins.LoadPlugins(initializer.ExcludedPlugins);
    }

    /// <summary>
    /// Reset model.
    /// </summary>
    [SecurityCritical]
    public static void Reset()
    {
      if (Interlocked.Exchange(ref _chat, null) == null)
        throw new InvalidOperationException("model not yet inited");

      Dispose(Client);
      Dispose(Peer);
      Dispose(Recorder);
      Dispose(Player);
      Dispose(Plugins);

      Client = null;
      Peer = null;
      Api = null;
    }

    [SecurityCritical]
    private static void Dispose(IDisposable disposable)
    {
      if (disposable == null)
        return;

      disposable.Dispose();
    }

    /// <summary>
    /// Check model initialization.
    /// If model not initialized then it will throw exception.
    /// </summary>
    [SecurityCritical]
    public static void Check()
    {
      if (!IsInited)
        throw new InvalidOperationException("Client not inited");
    }
  }
}
