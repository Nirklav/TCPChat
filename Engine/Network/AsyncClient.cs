using Engine.Api;
using Engine.Api.Server;
using Engine.Exceptions;
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
  /// Клиентское соединение.
  /// </summary>
  public sealed class AsyncClient : Connection
  {
    #region consts
    public const long DefaultFilePartSize = 1024 * 1024;
    private const int SystemTimerInterval = 1000;
    private const int ReconnectTimeInterval = 10 * 1000;
    private const int PingInterval = 3000;
    private const string ClientId = "Client";

    private static readonly SocketError[] reconnectErrors =
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
    [SecurityCritical] private IPEndPoint _hostAddress;
    [SecurityCritical] private ClientRequestQueue _requestQueue;
    [SecurityCritical] private Timer _timer;
    [SecurityCritical] private bool _reconnect;
    [SecurityCritical] private bool _reconnecting;
    [SecurityCritical] private DateTime _lastReconnect;
    [SecurityCritical] private DateTime _lastPingRequest;
    #endregion

    #region constructors
    /// <summary>
    /// Создает клиентское подключение к серверу.
    /// </summary>
    [SecurityCritical]
    public AsyncClient(string id)
    {
      _requestQueue = new ClientRequestQueue();
      _timer = new Timer(OnTimer, null, SystemTimerInterval, -1);
      _reconnecting = false;
      _reconnect = true;
      Id = id;
    }
    #endregion

    #region properties/events
    /// <summary>
    /// Подключен клиент к серверу, или нет.
    /// </summary>
    public bool IsConnected
    {
      [SecuritySafeCritical]
      get { return _handler == null ? false : _handler.Connected; }
    }

    /// <summary>
    /// Задает или возращает значение которое характеризует будет 
    /// ли клиент после потери связи пытатся пересоеденится с сервером.
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
    /// Асинхронно соединяет клиент с сервером.
    /// </summary>
    /// <param name="hostAddress">Адресс сервера.</param>
    [SecurityCritical]
    public void Connect(IPEndPoint hostAddress)
    {
      ThrowIfDisposed();

      if (_handler != null && _handler.Connected)
        throw new InvalidOperationException("Client already connected");

      _hostAddress = hostAddress;

      _handler = new Socket(hostAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      _handler.BeginConnect(hostAddress, OnConnected, null);
    }
    #endregion

    #region private/protected override methods
    [SecurityCritical]
    private void OnConnected(IAsyncResult result)
    {
      if (_disposed)
        return;

      try
      {
        _handler.EndConnect(result);

        Construct(_handler);
      }
      catch (SocketException se)
      {
        if (reconnectErrors.Contains(se.SocketErrorCode))
          _reconnecting = true;
        else
        {
          ClientModel.Notifier.Connected(new ConnectEventArgs(se));
          ClientModel.Logger.Write(se);
        }
      }
      catch (Exception e)
      {
        ClientModel.Notifier.Connected(new ConnectEventArgs(e));
        ClientModel.Logger.Write(e);
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

        var command = ClientModel.Api.GetCommand(e.Unpacked.Package.Id);
        var args = new ClientCommandArgs(null, e.Unpacked);

        _requestQueue.Add(ClientId, command, args);
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
        throw new InvalidOperationException("info isn't ClientConnectionInfo");

      if (!string.Equals(clientInfo.ApiName, ClientModel.Api.Name, StringComparison.OrdinalIgnoreCase))
        throw new ModelException(ErrorCode.APINotSupported, clientInfo.ApiName);

      ClientModel.Notifier.Connected(new ConnectEventArgs());
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

      if (!reconnectErrors.Contains(se.SocketErrorCode))
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
      if (!IsConnected || ClientModel.Api == null)
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
      if (interval < ReconnectTimeInterval)
        return;

      var args = new ReceiveMessageEventArgs
      {
        SystemMessage = SystemMessageId.ConnectionRetryAttempt,
        Time = DateTime.UtcNow,
        Type = MessageType.System
      };

      ClientModel.Notifier.ReceiveMessage(args);

      Clean();
      Connect(_hostAddress);

      _reconnecting = false;
      _lastReconnect = DateTime.UtcNow;
    }

    [SecurityCritical]
    private void OnError(Exception e)
    {
      ClientModel.Notifier.AsyncError(new AsyncErrorEventArgs(e));
      ClientModel.Logger.Write(e);
    }
    #endregion

    #region IDisposable

    [SecuritySafeCritical]
    protected override void Clean()
    {
      base.Clean();

      _requestQueue.Clean();
    }

    [SecuritySafeCritical]
    protected override void DisposeManagedResources()
    {
      base.DisposeManagedResources();

      if (_requestQueue != null)
      {
        _requestQueue.Dispose();
        _requestQueue = null;
      }

      lock (_syncObject)
      {
        if (_timer != null)
          _timer.Dispose();

        _timer = null;
      }
    }

    #endregion
  }
}
