using Engine.API;
using Engine.Model.Server;
using Engine.Network.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;

namespace Engine.Network
{
  public sealed class AsyncServer :
    MarshalByRefObject,
    IDisposable
  {
    #region const
    private const int ListenConnections = 100;
    private const int SystemTimerInterval = 1000;
    public const int MaxDataSize = 2 * 1024 * 1024; // 2 Мб
    #endregion

    #region fields
    [SecurityCritical] private readonly Dictionary<string, ServerConnection> connections;
    [SecurityCritical] private readonly ServerRequestQueue requestQueue;

    [SecurityCritical] private P2PService p2pService;
    [SecurityCritical] private Socket listener;

    [SecurityCritical] private bool isServerRunning;
    [SecurityCritical] private long lastTempId;
    [SecurityCritical] private bool disposed;

    [SecurityCritical] private readonly object timerSync = new object();
    [SecurityCritical] private Timer systemTimer;
    #endregion

    #region properties and events
    /// <summary>
    /// Возвращает true если сервер запущен.
    /// </summary>
    public bool IsServerRunning
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return isServerRunning;
      }
    }

    /// <summary>
    /// Сервис используемый для прямого соединения пользователей.
    /// </summary>
    internal P2PService P2PService
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return p2pService;
      }
    }

    /// <summary>
    /// Использует ли сервер IPv6 адреса. Если нет, используется IPv4.
    /// </summary>
    public bool UsingIPv6
    {
      [SecurityCritical]
      get { return listener.AddressFamily == AddressFamily.InterNetworkV6; }
    }
    #endregion

    #region constructors
    [SecurityCritical]
    public AsyncServer()
    {
      connections = new Dictionary<string, ServerConnection>();
      requestQueue = new ServerRequestQueue();
      isServerRunning = false;
    }
    #endregion

    #region public methods
    /// <summary>
    /// Включает сервер.
    /// </summary>
    /// <param name="serverPort">TCP порт для соединение с сервером.</param>
    /// <param name="p2pServicePort">Порт UDP P2P сервиса.</param>
    /// <param name="usingIPv6">Использовать ли IPv6, при ложном значении будет использован IPv4.</param>
    /// <exception cref="System.ArgumentException"/>
    [SecurityCritical]
    public void Start(int serverPort, int p2pServicePort, bool usingIPv6)
    {
      ThrowIfDisposed();

      if (isServerRunning)
        return;

      if (!Connection.TcpPortIsAvailable(serverPort))
        throw new ArgumentException("port not available", "serverPort");

      p2pService = new P2PService(p2pServicePort, usingIPv6);
      systemTimer = new Timer(OnTimer, null, SystemTimerInterval, -1);

      var address = usingIPv6 ? IPAddress.IPv6Any : IPAddress.Any;

      listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      listener.LingerState = new LingerOption(false, 0);
      listener.Bind(new IPEndPoint(address, serverPort));
      listener.Listen(ListenConnections);
      listener.BeginAccept(OnAccept, null);

      isServerRunning = true;
    }

    /// <summary>
    /// Регистрирует соединение.
    /// </summary>
    /// <param name="tempId">Временный идентификатор соединения.</param>
    /// <param name="id">Новый идентификатор соединения.</param>
    /// <param name="openKey">Публичный ключ соединения.</param>
    [SecurityCritical]
    public void RegisterConnection(string tempId, string id)
    {
      lock (connections)
      {
        var connection = GetConnection(tempId, true);
        if (connection == null)
          return;

        connections.Remove(tempId);
        connections.Add(id, connection);
        connection.Register(id);
      }
    }

    /// <summary>
    /// Закрывает соединение.
    /// </summary>
    /// <param name="id">Id cоединения, которое будет закрыто.</param>
    [SecuritySafeCritical]
    public void CloseConnection(string id)
    {
      P2PService.RemoveEndPoint(id);
      lock (connections)
      {
        var connection = GetConnection(id, true);
        if (connection == null)
          return;

        connections.Remove(id);
        connection.Dispose();
      }
    }

    /// <summary>
    /// Отсправляет пакет клиенту.
    /// </summary>
    /// <param name="connectionId">Id соединения.</param>
    /// <param name="id">Тип пакета. (Command.Id)</param>
    /// <param name="allowTempConnections">Разрешить незарегестрированные соединения.</param>
    [SecuritySafeCritical]
    public void SendMessage(string connectionId, long id, bool allowTempConnections = false)
    {
      SendMessage(connectionId, new Package(id), allowTempConnections);
    }

    /// <summary>
    /// Отсправляет пакет клиенту.
    /// </summary>
    /// <param name="connectionId">Id соединения.</param>
    /// <param name="id">Тип пакета. (Command.Id)</param>
    /// <param name="content">Контект команды.</param>
    /// <param name="allowTempConnections">Разрешить незарегестрированные соединения.</param>
    [SecuritySafeCritical]
    public void SendMessage<T>(string connectionId, long id, T content, bool allowTempConnections = false)
    {
      SendMessage(connectionId, new Package<T>(id, content), allowTempConnections);
    }

    /// <summary>
    /// Отсправляет пакет клиенту.
    /// </summary>
    /// <param name="connectionId">Id соединения.</param>
    /// <param name="package">Отправляемый пакет.</param>
    /// <param name="allowTempConnections">Разрешить незарегестрированные соединения.</param>
    [SecuritySafeCritical]
    public void SendMessage(string connectionId, IPackage package, bool allowTempConnections = false)
    {
      lock (connections)
      {
        var connection = GetConnection(connectionId, allowTempConnections);
        if (connection != null)
          connection.SendMessage(package);
      }
    }

    /// <summary>
    /// Возвращает список зарегестрированых Id соединений.
    /// </summary>
    /// <returns>Список зарегестрированых Id.</returns>
    [SecuritySafeCritical]
    public string[] GetConnetionsIds()
    {
      lock (connections)
        return connections.Keys.Where(id => !id.Contains(Connection.TempConnectionPrefix)).ToArray();
    }

    /// <summary>
    /// Проверяет если на сервер соединение с таким Id.
    /// </summary>
    /// <param name="id">Id соединения.</param>
    /// <returns>Есть ли соединение.</returns>
    [SecuritySafeCritical]
    public bool ContainsConnection(string id)
    {
      lock (connections)
        return connections.ContainsKey(id);
    }
    #endregion

    #region private callback methods
    [SecurityCritical]
    private void OnAccept(IAsyncResult result)
    {
      if (!isServerRunning)
        return;

      try
      {
        listener.BeginAccept(OnAccept, null);

        var handler = listener.EndAccept(result);
        var connection = new ServerConnection(handler, ServerModel.Api.Name, OnPackageReceived);

        connection.SendInfo();

        lock (connections)
        {
          connection.Id = string.Format("{0}{1}", Connection.TempConnectionPrefix, lastTempId++);
          connections.Add(connection.Id, connection);
        }
      }
      catch (Exception e)
      {
        ServerModel.Logger.Write(e);
      }
    }

    [SecurityCritical]
    private void OnPackageReceived(object sender, PackageReceivedEventArgs e)
    {
      try
      {
        if (e.Exception != null)
        {
          ServerModel.Logger.Write(e.Exception);
          return;
        }

        if (!isServerRunning)
          return;

        var connectionId = ((ServerConnection)sender).Id;
        var command = ServerModel.Api.GetCommand(e.Package.Id);
        var args = new ServerCommandArgs(connectionId, e.Package);

        requestQueue.Add(connectionId, command, args);
      }
      catch (Exception exc)
      {
        ServerModel.Logger.Write(exc);
      }
    }

    #region timer process
    [SecurityCritical]
    private void OnTimer(object arg)
    {
      RefreshConnections();
      RefreshRooms();

      lock (timerSync)
        if (systemTimer != null)
          systemTimer.Change(SystemTimerInterval, -1);
    }

    [SecurityCritical]
    private void RefreshConnections()
    {
      HashSet<string> removingUsers = null; // Prevent deadlock (in RemoveUser locked ServerModel)

      lock (connections)
      {
        var keys = connections.Keys.ToArray();
        foreach (var id in keys)
        {
          try
          {
            var connection = connections[id];
            if (connection.UnregisteredTimeInterval >= ServerConnection.UnregisteredTimeOut)
            {
              CloseConnection(id);
              continue;
            }

            if (connection.IntervalOfSilence >= ServerConnection.ConnectionTimeOut)
            {
              if (removingUsers == null)
                removingUsers = new HashSet<string>();
              removingUsers.Add(id);
              continue;
            }
          }
          catch (Exception e)
          {
            ServerModel.Logger.Write(e);
          }
        }
      }

      if (removingUsers != null)
      {
        foreach (var id in removingUsers)
        {
          try
          {
            ServerModel.Api.RemoveUser(id);
          }
          catch (Exception e)
          {
            ServerModel.Logger.Write(e);
            CloseConnection(id);
          }
        }
      }
    }

    [SecurityCritical]
    private void RefreshRooms()
    {
      if (!ServerModel.IsInited)
        return;

      using (var server = ServerModel.Get())
      {
        var roomsNames = server.Rooms.Keys.ToArray();
        foreach (var name in roomsNames)
        {
          if (name == ServerModel.MainRoomName)
            continue;

          var room = server.Rooms[name];
          if (room.Users.Count == 0)
            server.Rooms.Remove(name);
        }
      }
    }
    #endregion
    #endregion

    #region private methods
    [SecurityCritical]
    private ServerConnection GetConnection(string connectionId, bool allowTempConnections = false)
    {
      if (connectionId.Contains(Connection.TempConnectionPrefix) && !allowTempConnections)
        throw new InvalidOperationException("this connection don't registered");

      ServerConnection connection;
      if (!connections.TryGetValue(connectionId, out connection))
      {
        ServerModel.Logger.WriteWarning("Connection {0} don't finded", connectionId);
        return null;
      }

      return connection;
    }
    #endregion

    #region IDisposable
    [SecurityCritical]
    private void ThrowIfDisposed()
    {
      if (disposed)
        throw new ObjectDisposedException("Object disposed");
    }

    [SecurityCritical]
    private void DisposeManagedResources()
    {
      isServerRunning = false;

      if (requestQueue != null)
        requestQueue.Dispose();

      lock (timerSync)
      {
        if (systemTimer != null)
          systemTimer.Dispose();

        systemTimer = null;
      }

      lock (connections)
      {
        foreach (var connection in connections.Values)
          connection.Dispose();

        connections.Clear();
      }

      if (listener != null)
      {
        listener.Dispose();
        listener = null;
      }

      if (p2pService != null)
      {
        p2pService.Dispose();
        p2pService = null;
      }
    }

    /// <summary>
    /// Особождает все ресуры используемые сервером.
    /// </summary>
    [SecuritySafeCritical]
    public void Dispose()
    {
      if (disposed)
        return;

      disposed = true;
      DisposeManagedResources();
    }
    #endregion
  }
}
