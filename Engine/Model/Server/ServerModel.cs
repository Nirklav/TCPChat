using Engine.Api;
using Engine.Api.Server;
using Engine.Helpers;
using Engine.Model.Common;
using Engine.Model.Server.Entities;
using Engine.Network;
using Engine.Plugins.Server;
using System;
using System.Security;
using System.Threading;

namespace Engine.Model.Server
{
  [SecurityCritical]
  public static class ServerModel
  {
    #region static model
    private static ServerChat _chat;
    private static IServerNotifier _notifier = NotifierGenerator.MakeInvoker<IServerNotifier>();
    private static Logger _logger = new Logger("Server.log");

    /// <summary>
    /// Серверный API
    /// </summary>
    public static IApi Api
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
    /// Logger.
    /// </summary>
    public static Logger Logger
    {
      [SecurityCritical]
      get { return _logger; }
    }

    /// <summary>
    /// Исользовать только с конструкцией using
    /// </summary>
    /// <example>using (var server = SeeverModel.Get()) { ... }</example>
    /// <returns>Возвращает и блокирует модель.</returns>
    [SecurityCritical]
    public static ServerGuard Get()
    {
      if (Interlocked.CompareExchange(ref _chat, null, null) == null)
        throw new ArgumentException("model do not inited yet");

      return new ServerGuard(_chat);
    }
    #endregion

    #region static methods
    
    public static bool IsInited
    {
      [SecurityCritical]
      get { return Interlocked.CompareExchange(ref _chat, null, null) != null; }
    }

    [SecurityCritical]
    public static void Init(ServerInitializer initializer)
    {
      if (Interlocked.CompareExchange(ref _chat, new ServerChat(), null) != null)
        throw new InvalidOperationException("model already inited");

      Api = new ServerApi(initializer.AdminPassword);
      Server = new AsyncServer(Api, _notifier, Logger);

      Plugins = new ServerPluginManager(initializer.PluginsPath);
      Plugins.LoadPlugins(initializer.ExcludedPlugins);
    }

    [SecurityCritical]
    public static void Reset()
    {
      if (Interlocked.Exchange(ref _chat, null) == null)
        throw new InvalidOperationException("model not yet inited");

      Dispose(Server);
      Dispose(Plugins);
      Dispose(Api);

      Server = null;
      Plugins = null;
      Api = null;
    }

    [SecurityCritical]
    private static void Dispose(IDisposable disposable)
    {
      if (disposable != null)
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
