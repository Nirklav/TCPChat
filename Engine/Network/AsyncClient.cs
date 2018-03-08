using Engine.Api;
using Engine.Api.Server.Others;
using Engine.Exceptions;
using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;

namespace Engine.Network
{
  /// <summary>
  /// Client connection.
  /// </summary>
  public sealed class AsyncClient : Connection
  {
    #region consts
    public const string ClientId = "Client";

    public const long DefaultFilePartSize = 1024 * 1024;
    private const int SystemTimerInterval = 1000;
    private const int ReconnectInterval = 10 * 1000;
    private const int PingInterval = 3000;
    
    private static readonly SocketError[] ReconnectErrors =
    {
      SocketError.NetworkUnreachable,
      SocketError.NetworkDown,
      SocketError.NetworkReset,
      SocketError.ConnectionAborted,
      SocketError.ConnectionReset,
      SocketError.TimedOut,
      SocketError.HostDown,
      SocketError.HostUnreachable    
    };

    #endregion

    #region private fields
    [SecurityCritical] private readonly object _syncObject = new object();

    [SecurityCritical] private readonly IApi _api;
    [SecurityCritical] private readonly RequestQueue _requestQueue;
    [SecurityCritical] private readonly IClientNotifier _notifier;   
    [SecurityCritical] private readonly Timer _timer;
    [SecurityCritical] private readonly Logger _logger;

    [SecurityCritical] private IPEndPoint _hostAddress;
    [SecurityCritical] private bool _connecting;
    [SecurityCritical] private bool _reconnect;
    [SecurityCritical] private bool _reconnecting;
    [SecurityCritical] private DateTime _lastReconnect;
    [SecurityCritical] private DateTime _lastPingRequest;
    #endregion

    #region constructors
    /// <summary>
    /// Creates client connection.
    /// </summary>
    [SecurityCritical]
    public AsyncClient(string id, IApi api, IClientNotifier notifier, Logger logger)
      : base(logger)
    {
      Id = id;

      _api = api;
      _requestQueue = new RequestQueue(api);
      _requestQueue.Error += OnRequestQueueError;
      _notifier = notifier;

      _timer = new Timer(OnTimer, null, SystemTimerInterval, -1);
      _reconnect = true;
      _reconnecting = false;
      _logger = logger;
    }
    #endregion

    #region properties/events
    /// <summary>
    /// Sets or returns the value that will mean
    /// whether the client after a loss of communication to try to reconnect to the server.
    /// </summary>
    public bool Reconnect
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return _reconnect;
      }

      [SecurityCritical]
      set
      {
        ThrowIfDisposed();
        _reconnect = value;
      }
    }

    #endregion

    #region public methods
    /// <summary>
    /// Client connects to the server.
    /// </summary>
    /// <param name="hostAddress">Host address.</param>
    [SecurityCritical]
    public void Connect(IPEndPoint hostAddress)
    {
      ThrowIfDisposed();

      if (IsConnected || _connecting)
        throw new InvalidOperationException("Client already connected");

      _connecting = true;
      _hostAddress = hostAddress;

      Socket handler = new Socket(hostAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      handler.BeginConnect(hostAddress, OnConnected, handler);
    }
    #endregion

    #region private/protected override methods
    [SecurityCritical]
    private void OnConnected(IAsyncResult result)
    {
      if (IsClosed)
        return;

      try
      {
        Socket handler = (Socket)result.AsyncState;
        handler.EndConnect(result);
        _connecting = false;

        Construct(handler);
      }
      catch (SocketException se)
      {
        if (ReconnectErrors.Contains(se.SocketErrorCode))
          _reconnecting = true;
        else
        {
          _notifier.Connected(new ConnectEventArgs(se));
          _logger.Write(se);
        }
      }
      catch (Exception e)
      {
        _notifier.Connected(new ConnectEventArgs(e));
        _logger.Write(e);
      }
    }

    [SecuritySafeCritical]
    protected override void OnPackageReceived(PackageReceivedEventArgs e)
    {
      try
      {
        if (e.Exception != null)
        {
          OnError(e.Exception);
          return;
        }

        _requestQueue.Add(ClientId, e.Unpacked);
      }
      catch (Exception exc)
      {
        OnError(exc);
      }
    }

    [SecuritySafeCritical]
    protected override void OnInfoReceived(ConnectionInfo info)
    {
      var clientInfo = info as ServerConnectionInfo;
      if (clientInfo == null)
        throw new InvalidOperationException("info isn't ServerConnectionInfo");

      if (!string.Equals(clientInfo.ApiName, _api.Name, StringComparison.OrdinalIgnoreCase))
        throw new ModelException(ErrorCode.ApiNotSupported, clientInfo.ApiName);

      _notifier.Connected(new ConnectEventArgs());
    }

    [SecuritySafeCritical]
    protected override void OnPackageSent(PackageSendedEventArgs args)
    {
      if (args.Exception != null)
        OnError(args.Exception);
    }

    [SecuritySafeCritical]
    protected override bool OnSocketException(SocketException se)
    {
      if (!_reconnect)
        return false;

      if (!ReconnectErrors.Contains(se.SocketErrorCode))
        return false;

      _reconnecting = true;
      _lastReconnect = DateTime.UtcNow;
      return true;
    }

    [SecurityCritical]
    private void OnTimer(object state)
    {
      TrySendPingRequest();
      TryReconnect();

      lock (_syncObject)
        if (_timer != null)
          _timer.Change(SystemTimerInterval, -1);
    }

    [SecurityCritical]
    private void TrySendPingRequest()
    {
      if (!IsConnected)
        return;

      var interval = (DateTime.UtcNow - _lastPingRequest).TotalMilliseconds;
      if (interval < PingInterval)
        return;

      _lastPingRequest = DateTime.UtcNow;
      SendMessage(ServerPingRequestCommand.CommandId);
    }

    [SecurityCritical]
    private void TryReconnect()
    {
      if (!_reconnecting)
        return;

      var interval = (DateTime.UtcNow - _lastReconnect).TotalMilliseconds;
      if (interval < ReconnectInterval)
        return;

      var args = new ReceiveMessageEventArgs
      {
        SystemMessage = SystemMessageId.ConnectionRetryAttempt,
        Time = DateTime.UtcNow,
        Type = MessageType.System
      };

      _notifier.ReceiveMessage(args);

      Clean();
      Connect(_hostAddress);

      _reconnecting = false;
      _lastReconnect = DateTime.UtcNow;
    }

    [SecurityCritical]
    private void OnError(Exception e)
    {
      _notifier.AsyncError(new AsyncErrorEventArgs(e));
      _logger.Write(e);
    }

    [SecurityCritical]
    private void OnRequestQueueError(object sender, AsyncErrorEventArgs e)
    {
      _notifier.AsyncError(e);
      _logger.Write(e.Error);
    }
    #endregion

    #region IDisposable

    [SecuritySafeCritical]
    protected override void Clean()
    {
      base.Clean();

      _connecting = false;
      _requestQueue.Clean();
    }

    [SecuritySafeCritical]
    protected override void DisposeManagedResources()
    {
      base.DisposeManagedResources();

      if (_requestQueue != null)
        _requestQueue.Dispose();

      lock (_syncObject)
      {
        if (_timer != null)
          _timer.Dispose();
      }
    }

    #endregion
  }
}
