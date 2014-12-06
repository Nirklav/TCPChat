using Engine.Audio;
using Engine.Audio.OpenAL;
using Engine.Helpers;
using Engine.Model.Entities;
using Engine.Network;
using Engine.Plugins.Client;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Engine.Model.Client
{
  public class ClientModel
  {
    #region static model
    private static ClientModel model;
    private static Logger logger = new Logger("Client.log");
    private static IPlayer player = new OpenALPlayer();
    private static IRecorder recorder = new OpenALRecorder();
    private static ClientNotifier notifier = new ClientNotifier();

    public static Logger Logger { get { return logger; } }

    /// <summary>
    /// Клиентский API
    /// </summary>
    public static IClientAPI API { get; set; }

    /// <summary>
    /// Клиент
    /// </summary>
    public static AsyncClient Client { get; private set; }

    /// <summary>
    /// Пир 
    /// </summary>
    public static AsyncPeer Peer { get; private set; }

    /// <summary>
    /// Интерфейс для воспроизведения голоса.
    /// </summary>
    public static IPlayer Player { get { return player; } }

    /// <summary>
    /// Интерфейс для записи голоса с микрофона.
    /// </summary>
    public static IRecorder Recorder { get { return recorder; } }

    /// <summary>
    /// Менеджер плагинов.
    /// </summary>
    public static ClientPluginManager Plugins { get; private set; }

    /// <summary>
    /// Уведомитель.
    /// </summary>
    public static ClientNotifier Notifier { get { return notifier; } }

    /// <summary>
    /// Исользовать только с конструкцией using
    /// </summary>
    /// <example>using (var client = ClientModel.Get()) { ... }</example>
    /// <returns>Возвращает и блокирует модель.</returns>
    public static ClientContext Get()
    {
      if (Interlocked.CompareExchange(ref model, null, null) == null)
        throw new ArgumentException("model do not inited yet");

      return new ClientContext(model);
    }
    #endregion

    #region properties
    public User User { get; private set; }
    public Dictionary<string, Room> Rooms { get; private set; }
    public List<DownloadingFile> DownloadingFiles { get; private set; }
    public List<PostedFile> PostedFiles { get; private set; }
    #endregion

    #region conctructor
    public ClientModel()
    {
      Rooms = new Dictionary<string, Room>();
      DownloadingFiles = new List<DownloadingFile>();
      PostedFiles = new List<PostedFile>();
    }
    #endregion

    #region static methods
    public static bool IsInited
    {
      get { return Interlocked.CompareExchange(ref model, null, null) != null; }
    }

    public static void Init(ClientInitializer initializer)
    {
      if (Interlocked.CompareExchange(ref model, new ClientModel(), null) != null)
        throw new InvalidOperationException("model already inited");

      using (var client = Get())
        model.User = new User(initializer.Nick, initializer.NickColor);

      // API установится автоматически при подключении к серверу (согласно версии на сервере)
      Client = new AsyncClient(initializer.Nick);
      Peer = new AsyncPeer();
      Plugins = new ClientPluginManager(initializer.PluginsPath);
      Plugins.LoadPlugins(initializer.ExcludedPlugins);
    }

    public static void Reset()
    {
      if (Interlocked.Exchange(ref model, null) == null)
        throw new InvalidOperationException("model not yet inited");

      Dispose(Plugins);
      Dispose(Client);
      Dispose(Peer);
      Dispose(Recorder);
      Dispose(Player);

      Client = null;
      Peer = null;
      API = null;
    }

    private static void Dispose(IDisposable disposable)
    {
      if (disposable == null)
        return;

      disposable.Dispose();
    }
    #endregion
  }
}
