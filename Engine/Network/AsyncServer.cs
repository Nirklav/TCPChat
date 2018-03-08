using Engine.Api;
using Engine.Helpers;
using Engine.Model.Server;
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
    #endregion

    #region fields
    [SecurityCritical] private readonly Dictionary<string, ServerConnection> _connections;
    [SecurityCritical] private readonly Dictionary<string, IPAddress> _bannedIps;

    [SecurityCritical] private readonly IApi _api;
    [SecurityCritical] private readonly RequestQueue _requestQueue;
    [SecurityCritical] private readonly IServerNotifier _notifier;
    [SecurityCritical] private readonly ServerBans _bans;

    [SecurityCritical] private P2PService _p2pService;
    [SecurityCritical] private Socket _listener;

    [SecurityCritical] private bool _isServerRunning;
    [SecurityCritical] private long _lastTempId;
    [SecurityCritical] private bool _disposed;

    [SecurityCritical] private readonly object _timerSync = new object();
    [SecurityCritical] private Timer _systemTimer;

    [SecurityCritical] private readonly Logger _logger;
    #endregion

    #region properties
    /// <summary>
    /// Returns true if server is runned.
    /// </summary>
    public bool IsServerRunning
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return _isServerRunning;
      }
    }

    /// <summary>
    /// P2P service that used for direct users connecting.
    /// </summary>
    internal P2PService P2PService
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return _p2pService;
      }
    }

    /// <summary>
    /// Returns true if server use IPv6, otherwise false.
    /// </summary>
    public bool UsingIPv6
    {
      [SecurityCritical]
      get { return _listener.AddressFamily == AddressFamily.InterNetworkV6; }
    }

    /// <summary>
    /// Returns class that responses for bans.
    /// </summary>
    public ServerBans Bans
    {
      [SecuritySafeCritical]
      get { return _bans; }
    }
    #endregion

    #region constructors
    [SecurityCritical]
    public AsyncServer(IApi api, IServerNotifier notifier, Logger logger)
    {
      _connections = new Dictionary<string, ServerConnection>();

      _api = api;
      _requestQueue = new RequestQueue(api);
      _requestQueue.Error += OnRequestQueueError;
      _notifier = notifier;
      _bans = new ServerBans(this);

      _isServerRunning = false;
      _logger = logger;
    }
    #endregion

    #region public methods
    /// <summary>
    /// Starts the server.
    /// </summary>
    /// <param name="serverPort">TCP port that server be listening.</param>
    /// <param name="p2pServicePort">UDP port that p2p service be listening.</param>
    /// <param name="usingIPv6">Use the ipv6.</param>
    [SecurityCritical]
    public void Start(int serverPort, int p2pServicePort, bool usingIPv6)
    {
      ThrowIfDisposed();

      if (_isServerRunning)
        return;

      if (!Connection.TcpPortIsAvailable(serverPort))
        throw new ArgumentException("port not available", "serverPort");

      _p2pService = new P2PService(_api, _logger, p2pServicePort, usingIPv6);
      _systemTimer = new Timer(OnTimer, null, SystemTimerInterval, -1);

      var address = usingIPv6 ? IPAddress.IPv6Any : IPAddress.Any;

      _listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      _listener.LingerState = new LingerOption(false, 0);
      _listener.Bind(new IPEndPoint(address, serverPort));
      _listener.Listen(ListenConnections);
      _listener.BeginAccept(OnAccept, null);

      _isServerRunning = true;
    }

    /// <summary>
    /// Register the connections.
    /// </summary>
    /// <param name="tempId">Previous connection id.</param>
    /// <param name="id">New connection id.</param>
    [SecurityCritical]
    public void RegisterConnection(string tempId, string id)
    {
      lock (_connections)
      {
        var connection = GetConnection(tempId, true);
        if (connection == null)
          return;

        _connections.Remove(tempId);
        _connections.Add(id, connection);
        connection.Register(id);
      }
    }

    /// <summary>
    /// Close the connection.
    /// </summary>
    /// <param name="id">Connection id.</param>
    [SecuritySafeCritical]
    public void CloseConnection(string id)
    {
      P2PService.RemoveEndPoint(id);
      lock (_connections)
      {
        var connection = GetConnection(id, true);
        if (connection == null)
          return;

        _connections.Remove(id);
        connection.Dispose();
      }
    }

    /// <summary>
    /// Send package to client.
    /// </summary>
    /// <param name="connectionId">Connection id.</param>
    /// <param name="id">Package id. (Command.Id)</param>
    /// <param name="allowTempConnections">Allow not registered connection.</param>
    [SecuritySafeCritical]
    public void SendMessage(string connectionId, long id, bool allowTempConnections = false)
    {
      SendMessage(connectionId, new Package(id), allowTempConnections);
    }

    /// <summary>
    /// Send package to client.
    /// </summary>
    /// <param name="connectionId">Connection id.</param>
    /// <param name="id">Package id. (Command.Id)</param>
    /// <param name="content">Command content.</param>
    /// <param name="allowTempConnections">Allow not registered connection.</param>
    [SecuritySafeCritical]
    public void SendMessage<T>(string connectionId, long id, T content, bool allowTempConnections = false)
    {
      SendMessage(connectionId, new Package<T>(id, content), allowTempConnections);
    }

    /// <summary>
    /// Send package to client.
    /// </summary>
    /// <param name="connectionId">Connection id.</param>
    /// <param name="package">Sending package.</param>
    /// <param name="allowTempConnections">Allow not registered connection.</param>
    [SecuritySafeCritical]
    public void SendMessage(string connectionId, IPackage package, bool allowTempConnections = false)
    {
      lock (_connections)
      {
        var connection = GetConnection(connectionId, allowTempConnections);
        if (connection != null)
          connection.SendMessage(package);
      }
    }

    /// <summary>
    /// Retuns list of registered ids.
    /// </summary>
    [SecuritySafeCritical]
    public string[] GetConnetionsIds()
    {
      lock (_connections)
        return _connections.Keys.Where(id => !id.Contains(Connection.TempConnectionPrefix)).ToArray();
    }

    /// <summary>
    /// Checks whether a connection exists.
    /// </summary>
    /// <param name="id">Connection id.</param>
    /// <returns>If connections exist true will be returned, otherwise false.</returns>
    [SecuritySafeCritical]
    public bool ContainsConnection(string id)
    {
      lock (_connections)
        return _connections.ContainsKey(id);
    }
    
    /// <summary>
    /// Gets ip address of connection.
    /// </summary>
    [SecuritySafeCritical]
    public IPAddress GetIp(string connectionId)
    {
      lock (_connections)
      {
        if (!_connections.TryGetValue(connectionId, out ServerConnection connection))
          throw new ArgumentException("connection not found");
        return connection.RemotePoint.Address;
      }
    }
    #endregion

    #region private callback methods
    [SecurityCritical]
    private void OnAccept(IAsyncResult result)
    {
      if (!_isServerRunning)
        return;

      try
      {
        _listener.BeginAccept(OnAccept, null);

        var handler = _listener.EndAccept(result);
        var endPoint = (IPEndPoint)handler.RemoteEndPoint;

        if (_bans.IsBanned(endPoint.Address))
        {
          handler.Disconnect(false);
          handler.Dispose();
        }
        else
        {
          var connection = new ServerConnection(handler, _api.Name, _logger, OnPackageReceived);
          connection.SendInfo();

          lock (_connections)
          {
            connection.Id = string.Format("{0}{1}", Connection.TempConnectionPrefix, _lastTempId++);
            _connections.Add(connection.Id, connection);
          }

          _notifier.ConnectionOpened(new ConnectionEventArgs(connection.Id));
        }
      }
      catch (Exception e)
      {
        _logger.Write(e);
      }
    }

    [SecurityCritical]
    private void OnPackageReceived(object sender, PackageReceivedEventArgs e)
    {
      try
      {
        if (e.Exception != null)
        {
          _logger.Write(e.Exception);
          return;
        }

        if (!_isServerRunning)
          return;

        var connectionId = ((ServerConnection)sender).Id;
        _requestQueue.Add(connectionId, e.Unpacked);
      }
      catch (Exception exc)
      {
        _logger.Write(exc);
      }
    }

    [SecurityCritical]
    private void OnRequestQueueError(object sender, AsyncErrorEventArgs e)
    {
      _logger.Write(e.Error);
    }

    #region timer process
    [SecurityCritical]
    private void OnTimer(object arg)
    {
      try
      {
        RefreshConnections();
      }
      catch (Exception e)
      {
        _logger.Write(e);
      }

      lock (_timerSync)
        if (_systemTimer != null)
          _systemTimer.Change(SystemTimerInterval, -1);
    }

    [SecurityCritical]
    private void RefreshConnections()
    {
      HashSet<string> removing = null; // Prevent deadlock

      lock (_connections)
      {
        var keys = _connections.Keys.ToArray();
        foreach (var id in keys)
        {
          try
          {
            var connection = _connections[id];
            if (connection.UnregisteredInterval >= ServerConnection.UnregisteredTimeout)
            {
              // Just close connection, user not registered yet.
              CloseConnection(id);
              continue;
            }

            if (connection.SilenceInterval >= ServerConnection.SilenceTimeout)
            {
              if (removing == null)
                removing = new HashSet<string>();
              removing.Add(id);
            }
          }
          catch (Exception e)
          {
            _logger.Write(e);
          }
        }
      }

      if (removing != null)
      {
        foreach (var id in removing)
        {
          var closure = new ConnectionClosingClosure(this, id);
          _notifier.ConnectionClosing(new ConnectionEventArgs(id), closure.Callback);
        }
      }
    }

    private class ConnectionClosingClosure
    {
      private readonly AsyncServer _server;
      private readonly string _id;

      public ConnectionClosingClosure(AsyncServer server, string id)
      {
        _id = id;
        _server = server;
      }

      [SecuritySafeCritical]
      public void Callback(Exception e)
      {
        try
        {
          _server.CloseConnection(_id);
          _server._notifier.ConnectionClosed(new ConnectionEventArgs(_id));

          if (e != null)
            _server._logger.Write(e);
        }
        catch(Exception e2)
        {
          _server._logger.Write(e2);
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

      if (!_connections.TryGetValue(connectionId, out ServerConnection connection))
      {
        _logger.WriteWarning("Connection {0} don't finded", connectionId);
        return null;
      }

      return connection;
    }
    #endregion

    #region IDisposable
    [SecurityCritical]
    private void ThrowIfDisposed()
    {
      if (_disposed)
        throw new ObjectDisposedException("Object disposed");
    }

    [SecurityCritical]
    private void DisposeManagedResources()
    {
      _isServerRunning = false;

      if (_requestQueue != null)
        _requestQueue.Dispose();

      lock (_timerSync)
      {
        if (_systemTimer != null)
          _systemTimer.Dispose();

        _systemTimer = null;
      }

      lock (_connections)
      {
        foreach (var connection in _connections.Values)
          connection.Dispose();

        _connections.Clear();
      }

      if (_listener != null)
      {
        _listener.Dispose();
        _listener = null;
      }

      if (_p2pService != null)
      {
        _p2pService.Dispose();
        _p2pService = null;
      }
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;
      DisposeManagedResources();
    }
    #endregion
  }
}
