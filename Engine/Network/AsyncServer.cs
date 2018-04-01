using Engine.Api;
using Engine.Helpers;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography.X509Certificates;
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
    [SecurityCritical] private readonly Dictionary<UserId, ServerConnection> _connections;

    [SecurityCritical] private readonly X509Certificate2 _certificate;
    [SecurityCritical] private readonly IApi _api;
    [SecurityCritical] private readonly RequestQueue _requestQueue;
    [SecurityCritical] private readonly IServerNotifier _notifier;
    [SecurityCritical] private readonly ServerBans _bans;

    [SecurityCritical] private P2PService _p2pService;
    [SecurityCritical] private int _port;
    [SecurityCritical] private int _p2pServicePort;
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
    public AsyncServer(X509Certificate2 certificate, IApi api, IServerNotifier notifier, Logger logger)
    {
      _connections = new Dictionary<UserId, ServerConnection>();

      _certificate = certificate;
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
    /// <param name="serverUri">Server address.</param>
    /// <param name="p2pServicePort">UDP port that p2p service be listening.</param>
    [SecurityCritical]
    public void Start(Uri serverUri, int p2pServicePort)
    {
      ThrowIfDisposed();

      if (_isServerRunning)
        throw new InvalidOperationException("Server already started");

      if (!serverUri.Scheme.Equals(Connection.TcpChatScheme, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Invalid host scheme");

      if (!Connection.TcpPortIsAvailable(serverUri.Port))
        throw new ArgumentException("Port not available", nameof(serverUri));

      try
      {
        switch (serverUri.HostNameType)
        {
          case UriHostNameType.IPv4:
          case UriHostNameType.IPv6:
            _isServerRunning = true;
            _port = serverUri.Port;
            _p2pServicePort = p2pServicePort;
            var address = IPAddress.Parse(serverUri.Host);
            StartImpl(address);
            break;
          case UriHostNameType.Dns:
            _isServerRunning = true;
            _port = serverUri.Port;
            _p2pServicePort = p2pServicePort;
            Dns.BeginGetHostAddresses(serverUri.DnsSafeHost, OnGetHostAddresses, null);
            break;
          default:
            throw new ArgumentException("Not supported uri address");
        }
      }
      catch (Exception)
      {
        _isServerRunning = false;
        _port = 0;
        _p2pServicePort = 0;

        throw;
      }
    }

    [SecurityCritical]
    private void OnGetHostAddresses(IAsyncResult result)
    {
      try
      {
        var addresses = Dns.EndGetHostAddresses(result);
        if (addresses.Length > 0)
        {
          var address = addresses[0];
          StartImpl(address);
        }
        else
        {
          throw new InvalidOperationException("Addresses are empty");
        }
      }
      catch (Exception e)
      {
        _isServerRunning = false;
        _notifier.StartError(new AsyncErrorEventArgs(e));
        _logger.Write(e);
      }
    }

    [SecurityCritical]
    private void StartImpl(IPAddress address)
    {
      _systemTimer = new Timer(OnTimer, null, SystemTimerInterval, -1);
      _p2pService = new P2PService(address, _p2pServicePort, _api, _logger);

      _listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      _listener.LingerState = new LingerOption(false, 0);
      _listener.Bind(new IPEndPoint(address, _port));
      _listener.Listen(ListenConnections);
      _listener.BeginAccept(OnAccept, null);
    }

    /// <summary>
    /// Register the connections.
    /// </summary>
    /// <param name="tempId">Previous connection id.</param>
    /// <param name="id">New connection id.</param>
    [SecurityCritical]
    public void RegisterConnection(UserId tempId, UserId id)
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
    public void CloseConnection(UserId id)
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
    public void SendMessage(UserId connectionId, long id, bool allowTempConnections = false)
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
    public void SendMessage<T>(UserId connectionId, long id, T content, bool allowTempConnections = false)
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
    public void SendMessage(UserId connectionId, IPackage package, bool allowTempConnections = false)
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
    public UserId[] GetConnetionsIds()
    {
      lock (_connections)
        return _connections.Keys.Where(id => !id.IsTemporary).ToArray();
    }

    /// <summary>
    /// Checks whether a connection exists.
    /// </summary>
    /// <param name="connectionId">Connection id.</param>
    /// <returns>If connections exist true will be returned, otherwise false.</returns>
    [SecuritySafeCritical]
    public bool ContainsConnection(UserId connectionId)
    {
      lock (_connections)
        return _connections.ContainsKey(connectionId);
    }
    
    /// <summary>
    /// Gets ip address of connection.
    /// </summary>
    [SecuritySafeCritical]
    public IPAddress GetIp(UserId connectionId)
    {
      lock (_connections)
      {
        var connection = GetConnection(connectionId, true);
        return connection.RemotePoint.Address;
      }
    }

    /// <summary>
    /// Gets certificate of connection.
    /// </summary>
    [SecuritySafeCritical]
    public X509Certificate2 GetCertificate(UserId connectionId)
    {
      lock (_connections)
      {
        var connection = GetConnection(connectionId, true);
        return connection.RemoteCertiticate;
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
          var connection = new ServerConnection(handler, _certificate, _api.Name, _logger, OnPackageReceived);
          connection.SendServerInfo();

          lock (_connections)
          {
            connection.Id = new UserId(_lastTempId++);
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
        }
        else if (_isServerRunning)
        {
          var connectionId = ((ServerConnection)sender).Id;
          _requestQueue.Add(connectionId, e.Unpacked);
        }
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
      HashSet<UserId> removing = null; // Prevent deadlock

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
                removing = new HashSet<UserId>();
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
      private readonly UserId _id;

      public ConnectionClosingClosure(AsyncServer server, UserId id)
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
    private ServerConnection GetConnection(UserId connectionId, bool allowTempConnections = false)
    {
      if (connectionId.IsTemporary && !allowTempConnections)
        throw new InvalidOperationException("this connection don't registered");

      if (!_connections.TryGetValue(connectionId, out ServerConnection connection))
      {
        _logger.WriteWarning("Connection {0} not found", connectionId);
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
