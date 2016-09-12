using Engine.Api;
using Engine.Api.Client;
using Engine.Audio;
using Engine.Audio.OpenAL;
using Engine.Helpers;
using Engine.Model.Client.Entities;
using Engine.Model.Common;
using Engine.Model.Common.Entities;
using Engine.Network;
using Engine.Plugins.Client;
using System;
using System.Security;
using System.Threading;

namespace Engine.Model.Client
{
  [SecurityCritical]
  public class ClientModel
  {
    #region static model
    private static ClientModel _model;
    private static Logger _logger = new Logger("Client.log");
    private static IPlayer _player = new OpenALPlayer();
    private static IRecorder _recorder = new OpenALRecorder();
    private static IClientNotifier _notifier = NotifierGenerator.MakeInvoker<IClientNotifier>();

    public static Logger Logger
    {
      [SecurityCritical]
      get { return _logger; }
    }

    /// <summary>
    /// Клиентский API.
    /// </summary>
    public static ClientApi Api
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Клиент.
    /// </summary>
    public static AsyncClient Client
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Пир.
    /// </summary>
    public static AsyncPeer Peer
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Интерфейс для воспроизведения звука.
    /// </summary>
    public static IPlayer Player
    {
      [SecurityCritical]
      get { return _player; }
    }

    /// <summary>
    /// Интерфейс для записи звука.
    /// </summary>
    public static IRecorder Recorder
    {
      [SecurityCritical]
      get { return _recorder; }
    }

    /// <summary>
    /// Менеджер плагинов.
    /// </summary>
    public static ClientPluginManager Plugins
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Уведомитель.
    /// </summary>
    public static IClientNotifier Notifier
    {
      [SecurityCritical]
      get { return _notifier; }
    }

    /// <summary>
    /// Создает контекст и блокирует модель данных клиента. Исользовать только с конструкцией using.
    /// </summary>
    /// <example>using (var client = ClientModel.Get()) { ... }</example>
    /// <returns>Контекст данных модели.</returns>
    [SecurityCritical]
    public static ClientGuard Get()
    {
      if (Interlocked.CompareExchange(ref _model, null, null) == null)
        throw new ArgumentException("model do not inited yet");

      return new ClientGuard(_model);
    }
    #endregion

    #region static methods
    public static bool IsInited
    {
      [SecurityCritical]
      get { return Interlocked.CompareExchange(ref _model, null, null) != null; }
    }

    [SecurityCritical]
    public static void Init(ClientInitializer initializer)
    {
      var user = new ClientUser(initializer.Nick, initializer.NickColor);

      if (Interlocked.CompareExchange(ref _model, new ClientModel(user), null) != null)
        throw new InvalidOperationException("model already inited");

      Api = new ClientApi();
      Client = new AsyncClient(initializer.Nick);
      Peer = new AsyncPeer();

      Plugins = new ClientPluginManager(initializer.PluginsPath);
      Plugins.LoadPlugins(initializer.ExcludedPlugins);
    }

    [SecurityCritical]
    public static void Reset()
    {
      if (Interlocked.Exchange(ref _model, null) == null)
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

    [SecurityCritical]
    public static void Check()
    {
      if (!IsInited)
        throw new InvalidOperationException("Client not inited");
    }
    #endregion

    #region chat
    public ClientChat Chat
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }
    #endregion

    #region conctructor
    [SecurityCritical]
    public ClientModel(ClientUser user)
    {
      Chat = new ClientChat(user);
    }
    #endregion
  }
}
