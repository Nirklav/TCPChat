using Engine.API;
using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Network.Connections;
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
    public const long DefaultFilePartSize = 500 * 1024;
    private const int SystemTimerInterval = 1000;
    private const int ReconnectTimeInterval = 10 * 1000;
    private const int PingInterval = 3000;
    private const string ClientId = "Client";

    private static readonly SocketError[] reconnectErrors =
    { 
      SocketError.NetworkReset, SocketError.ConnectionAborted,
      SocketError.ConnectionReset, SocketError.TimedOut,
      SocketError.HostDown
    };

    #endregion

    #region private fields
    private readonly object syncObject = new object();
    private IPEndPoint hostAddress;
    private ClientRequestQueue requestQueue;
    private Timer timer;
    private bool reconnect;
    private bool reconnecting;
    private DateTime lastReconnect;
    private DateTime lastPingRequest;
    #endregion

    #region constructors
    /// <summary>
    /// Создает клиентское подключение к серверу.
    /// </summary>
    [SecurityCritical]
    public AsyncClient(string nick)
    {
      requestQueue = new ClientRequestQueue();
      reconnecting = false;
      reconnect = true;
      Id = nick;
    }
    #endregion

    #region properties/events
    /// <summary>
    /// Подключен клиент к серверу, или нет.
    /// </summary>
    public bool IsConnected
    {
      [SecuritySafeCritical]
      get { return handler == null ? false : handler.Connected; }
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
        return reconnect;
      }

      [SecurityCritical]
      set
      {
        ThrowIfDisposed();
        reconnect = value;
      }
    }

    #endregion

    #region public methods
    /// <summary>
    /// Асинхронно соединяет клиент с сервером.
    /// </summary>
    /// <param name="ServerAddress">Адресс сервера.</param>
    [SecurityCritical]
    public void Connect(IPEndPoint serverAddress)
    {
      ThrowIfDisposed();

      if (handler != null && handler.Connected)
        throw new InvalidOperationException("Client already connected");

      hostAddress = serverAddress;
      timer = new Timer(OnTimer, null, SystemTimerInterval, -1);

      handler = new Socket(serverAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      handler.BeginConnect(serverAddress, OnConnected, null);
    }
    #endregion

    #region private/protected override methods
    [SecurityCritical]
    private void OnConnected(IAsyncResult result)
    {
      if (disposed)
        return;

      try
      {
        handler.EndConnect(result);

        Construct(handler);
        reconnecting = false;
      }
      catch (SocketException se)
      {
        if (se.SocketErrorCode == SocketError.ConnectionRefused)
          reconnecting = true;
        else
        {
          ClientModel.Notifier.Connected(new ConnectEventArgs { Error = se });
          ClientModel.Logger.Write(se);
        }
      }
      catch (Exception e)
      {
        ClientModel.Notifier.Connected(new ConnectEventArgs { Error = e });
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

        var command = ClientModel.Api.GetCommand(e.Package.Id);
        var args = new ClientCommandArgs(null, e.Package);

        requestQueue.Add(ClientId, command, args);
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
      if (!reconnect)
        return false;

      if (!reconnectErrors.Contains(se.SocketErrorCode))
        return false;

      reconnecting = true;
      lastReconnect = DateTime.Now;
      return true;
    }

    [SecurityCritical]
    private void OnTimer(object state)
    {
      TrySendPingRequest();

      TryReconnect();

      lock (syncObject)
        if (timer != null)
          timer.Change(SystemTimerInterval, -1);
    }

    [SecurityCritical]
    private void TrySendPingRequest()
    {
      if (!IsConnected || ClientModel.Api == null)
        return;

      var interval = (DateTime.Now - lastPingRequest).TotalMilliseconds;
      if (interval < PingInterval)
        return;

      lastPingRequest = DateTime.Now;
      ClientModel.Api.PingRequest();
    }

    [SecurityCritical]
    private void TryReconnect()
    {
      if (!reconnecting)
        return;

      var interval = (DateTime.Now - lastReconnect).TotalMilliseconds;
      if (interval < ReconnectTimeInterval)
        return;

      ClientModel.Notifier.ReceiveMessage(new ReceiveMessageEventArgs { Message = "Попытка соединения с сервером...", Type = MessageType.System });

      if (handler != null)
        handler.Dispose();

      Connect(hostAddress);

      lastReconnect = DateTime.Now;
    }

    [SecurityCritical]
    private void OnError(Exception e)
    {
      ClientModel.Notifier.AsyncError(new AsyncErrorEventArgs { Error = e });
      ClientModel.Logger.Write(e);
    }
    #endregion

    #region IDisposable

    [SecuritySafeCritical]
    protected override void DisposeManagedResources()
    {
      base.DisposeManagedResources();

      if (requestQueue != null)
        requestQueue.Dispose();

      lock (syncObject)
      {
        if (timer != null)
          timer.Dispose();

        timer = null;
      }
    }

    #endregion
  }
}
