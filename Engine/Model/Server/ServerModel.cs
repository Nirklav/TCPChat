using Engine.Api;
using Engine.Helpers;
using Engine.Model.Common;
using Engine.Model.Entities;
using Engine.Network;
using Engine.Plugins.Server;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;

namespace Engine.Model.Server
{
  [SecurityCritical]
  public class ServerModel
  {
    #region static model
    private static ServerModel _model;
    private static Logger _logger = new Logger("Server.log");
    private static IServerNotifier _notifier = NotifierGenerator.MakeInvoker<IServerNotifier>();

    public static Logger Logger
    {
      [SecurityCritical]
      get { return _logger; }
    }

    /// <summary>
    /// Серверный API
    /// </summary>
    public static ServerApi Api
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Сервер
    /// </summary>
    public static AsyncServer Server
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Менеджер плагинов.
    /// </summary>
    public static ServerPluginManager Plugins
    {
      [SecurityCritical]
      get;
      [SecurityCritical]
      private set;
    }

    /// <summary>
    /// Уведомитель.
    /// </summary>
    public static IServerNotifier Notifier
    {
      [SecurityCritical]
      get { return _notifier; }
    }

    /// <summary>
    /// Исользовать только с конструкцией using
    /// </summary>
    /// <example>using (var server = SeeverModel.Get()) { ... }</example>
    /// <returns>Возвращает и блокирует модель.</returns>
    [SecurityCritical]
    public static ServerGuard Get()
    {
      if (Interlocked.CompareExchange(ref _model, null, null) == null)
        throw new ArgumentException("model do not inited yet");

      return new ServerGuard(_model);
    }
    #endregion

    #region consts
    public const string MainRoomName = "Main room";
    #endregion

    #region properties
    public Dictionary<string, Room> Rooms
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
    #endregion

    #region constructor
    [SecurityCritical]
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
      [SecurityCritical]
      get { return Interlocked.CompareExchange(ref _model, null, null) != null; }
    }

    [SecurityCritical]
    public static void Init(ServerInitializer initializer)
    {
      if (Interlocked.CompareExchange(ref _model, new ServerModel(), null) != null)
        throw new InvalidOperationException("model already inited");

      Server = new AsyncServer();
      Api = new ServerApi();

      Plugins = new ServerPluginManager(initializer.PluginsPath);
      Plugins.LoadPlugins(initializer.ExcludedPlugins);
    }

    [SecurityCritical]
    public static void Reset()
    {
      if (Interlocked.Exchange(ref _model, null) == null)
        throw new InvalidOperationException("model not yet inited");

      Dispose(Server);
      Dispose(Plugins);

      Server = null;
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
        throw new InvalidOperationException("Server not inited");
    }
    #endregion
  }
}
