using Engine.API;
using Engine.Audio;
using Engine.Audio.OpenAL;
using Engine.Helpers;
using Engine.Model.Common;
using Engine.Model.Entities;
using Engine.Network;
using Engine.Plugins.Client;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;

namespace Engine.Model.Client
{
  [SecurityCritical]
  public class ClientModel
  {
    #region static model
    private static ClientModel model;
    private static Logger logger = new Logger("Client.log");
    private static IPlayer player = new OpenALPlayer();
    private static IRecorder recorder = new OpenALRecorder();
    private static IClientNotifier notifier = NotifierGenerator.MakeInvoker<IClientNotifier>();

    public static Logger Logger
    {
      [SecurityCritical]
      get { return logger; }
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
      get { return player; }
    }

    /// <summary>
    /// Интерфейс для записи звука.
    /// </summary>
    public static IRecorder Recorder
    {
      [SecurityCritical]
      get { return recorder; }
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
      get { return notifier; }
    }

    /// <summary>
    /// Создает контекст и блокирует модель данных клиента. Исользовать только с конструкцией using.
    /// </summary>
    /// <example>using (var client = ClientModel.Get()) { ... }</example>
    /// <returns>Контекст данных модели.</returns>
    [SecurityCritical]
    public static ClientContext Get()
    {
      if (Interlocked.CompareExchange(ref model, null, null) == null)
        throw new ArgumentException("model do not inited yet");

      return new ClientContext(model);
    }
    #endregion

    #region properties
    public User User
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    public Dictionary<string, User> Users
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    public Dictionary<string, Room> Rooms
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    public List<DownloadingFile> DownloadingFiles
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    public List<PostedFile> PostedFiles
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }
    #endregion

    #region conctructor
    [SecurityCritical]
    public ClientModel(User user)
    {
      User = user;
      Users = new Dictionary<string, User>();
      Rooms = new Dictionary<string, Room>();
      DownloadingFiles = new List<DownloadingFile>();
      PostedFiles = new List<PostedFile>();
    }
    #endregion

    #region static methods
    public static bool IsInited
    {
      [SecurityCritical]
      get { return Interlocked.CompareExchange(ref model, null, null) != null; }
    }

    [SecurityCritical]
    public static void Init(ClientInitializer initializer)
    {
      var user = new User(initializer.Nick, initializer.NickColor);

      if (Interlocked.CompareExchange(ref model, new ClientModel(user), null) != null)
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
      if (Interlocked.Exchange(ref model, null) == null)
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
    #endregion
  }
}
