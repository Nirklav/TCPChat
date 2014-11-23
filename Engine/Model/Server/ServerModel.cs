using Engine.API.StandardAPI;
using Engine.Helpers;
using Engine.Model.Entities;
using Engine.Network;
using Engine.Plugins.Server;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Engine.Model.Server
{
  public class ServerModel
  {
    #region static model
    private static ServerModel model;
    private static Logger logger = new Logger("Server.log");
    private static ServerNotifier notifier = new ServerNotifier();

    public static Logger Logger { get { return logger; } }

    /// <summary>
    /// Серверный API
    /// </summary>
    public static IServerAPI API { get; private set; }

    /// <summary>
    /// Сервер
    /// </summary>
    public static AsyncServer Server { get; private set; }

    /// <summary>
    /// Менеджер плагинов.
    /// </summary>
    public static ServerPluginManager Plugins { get; private set; }

    /// <summary>
    /// Уведомитель.
    /// </summary>
    public static ServerNotifier Notifier { get { return notifier; } }

    /// <summary>
    /// Исользовать только с конструкцией using
    /// </summary>
    /// <example>using (var server = SeeverModel.Get()) { ... }</example>
    /// <returns>Возвращает и блокирует модель.</returns>
    public static ServerContext Get()
    {
      if (Interlocked.CompareExchange(ref model, null, null) == null)
        throw new ArgumentException("model do not inited yet");

      return new ServerContext(model);
    }
    #endregion

    #region consts
    public const string MainRoomName = "Main room";
    #endregion

    #region properties
    public Dictionary<string, Room> Rooms { get; private set; }
    public Dictionary<string, User> Users { get; private set; }
    #endregion

    #region constructor
    public ServerModel()
    {
      Users = new Dictionary<string, User>();
      Rooms = new Dictionary<string, Room>();

      Rooms.Add(MainRoomName, new Room(null, MainRoomName));
    }
    #endregion

    #region static methods
    public static bool IsInited
    {
      get { return Interlocked.CompareExchange(ref model, null, null) != null; }
    }

    public static void Init(ServerInitializer initializer)
    {
      if (Interlocked.CompareExchange(ref model, new ServerModel(), null) != null)
        throw new InvalidOperationException("model already inited");

      Server = new AsyncServer();
      API = initializer.API ?? new StandardServerAPI();

      Plugins = new ServerPluginManager(initializer.PluginsPath);
      Plugins.LoadPlugins(initializer.ExcludedPlugins);
    }

    public static void Reset()
    {
      if (Interlocked.Exchange(ref model, null) == null)
        throw new InvalidOperationException("model not yet inited");

      Plugins.UnloadPlugins();

      Dispose(Server);

      Server = null;
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
