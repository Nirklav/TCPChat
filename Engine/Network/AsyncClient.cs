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
using System.Security.Cryptography.X509Certificates;
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
    [SecurityCritical] private readonly Logger _logger;
    [SecurityCritical] private Timer _timer;

    [SecurityCritical] private Uri _serverUri;
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
    public AsyncClient(string id, X509Certificate2 certificate, IApi api, IClientNotifier notifier, Logger logger)
      : base(certificate, logger)
    {
      Id = id;

      _api = api;
      _requestQueue = new RequestQueue(api);
      _requestQueue.Error += OnRequestQueueError;
      _notifier = notifier;

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
    /// <param name="serverUri">Server uri.</param>
    [SecurityCritical]
    public void Connect(Uri serverUri)
    {
      ThrowIfDisposed();

      if (!serverUri.Scheme.Equals(TcpChatScheme, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Invalid host scheme");

      if (IsConnected || _connecting)
        throw new InvalidOperationException("Client already connected");

      switch (serverUri.HostNameType)
      {
        case UriHostNameType.IPv4:
        case UriHostNameType.IPv6:
          _connecting = true;
          _serverUri = serverUri;
          var address = IPAddress.Parse(serverUri.Host);
          ConnectImpl(address);
          break;
        case UriHostNameType.Dns:
          _connecting = true;
          _serverUri = serverUri;
          Dns.BeginGetHostAddresses(serverUri.DnsSafeHost, OnGetHostAddresses, null);
          break;
        default:
          throw new ArgumentException("Not supported uri address");
      }
    }
    #endregion

    #region private/protected override methods
    [SecurityCritical]
    private void OnGetHostAddresses(IAsyncResult result)
    {
      if (IsClosed)
        return;

      try
      {
        var addresses = Dns.EndGetHostAddresses(result);
        if (addresses.Length > 0)
        {
          var address = addresses[0];
          ConnectImpl(address);
        }
        else
        {
          throw new InvalidOperationException("Addresses are empty");
        }
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

    [SecurityCritical]
    private void ConnectImpl(IPAddress address)
    {
      var endPoint = new IPEndPoint(address, _serverUri.Port);
      var handler = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      handler.BeginConnect(endPoint, OnConnected, handler);
    }

    [SecurityCritical]
    private void OnConnected(IAsyncResult result)
    {
      if (IsClosed)
        return;

      try
      {
        var handler = (Socket)result.AsyncState;
        handler.EndConnect(result);
        
        Construct(handler, ConnectionState.ServerInfoWait);

        _connecting = false;
        _timer = new Timer(OnTimer, null, SystemTimerInterval, -1);
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
    protected override void OnServerInfo(ServerInfo info)
    {
      if (!string.Equals(info.ApiName, _api.Name, StringComparison.OrdinalIgnoreCase))
        throw new ModelException(ErrorCode.ApiNotSupported, info.ApiName);

      base.OnServerInfo(info);
    }

    [SecuritySafeCritical]
    protected override void OnHandshakeResponse(HandshakeResponse response)
    {
      base.OnHandshakeResponse(response);

      _notifier.Connected(new ConnectEventArgs());
    }

    [SecuritySafeCritical]
    protected override void OnHandshakeException(Exception e)
    {
      base.OnHandshakeException(e);

      _notifier.Connected(new ConnectEventArgs(e));
    }

    [SecuritySafeCritical]
    protected override bool ValidateCertificate(X509Certificate2 remote)
    {
      if (base.ValidateCertificate(remote))
      {
        var connectionHost = _serverUri.DnsSafeHost;
        var certificateHost = remote.GetNameInfo(X509NameType.DnsName, false);

        var wildcard = "*.";
        if (!certificateHost.StartsWith(wildcard))
          return string.Equals(connectionHost, certificateHost, StringComparison.OrdinalIgnoreCase);
        else
        {
          var connectionHostParts = connectionHost
            .Split('.')
            .Skip(1)
            .ToArray();

          var certificateHostParts = certificateHost
            .Substring(wildcard.Length)
            .Split('.');

          if (connectionHostParts.Length != certificateHostParts.Length)
            return false;

          for (int i = 0; i < connectionHostParts.Length; i++)
          {
            var connectionPart = connectionHostParts[i];
            var certiticatePart = certificateHostParts[i];

            if (!string.Equals(connectionPart, certiticatePart, StringComparison.OrdinalIgnoreCase))
              return false;
          }
          return true;
        }
      }
      return false;
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
      Connect(_serverUri);

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
