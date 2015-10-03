using Engine.API;
using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Network.Connections;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Engine.Network
{
  /// <summary>
  /// Клиентское соединение.
  /// </summary>
  public sealed class AsyncClient : Connection
  {
    #region consts
    public const int CryptorKeySize = 2048;
    public const long DefaultFilePartSize = 500 * 1024;
    private const int MaxReceivedDataSize = 1024 * 1024;
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
    private IPEndPoint hostAddress;
    private ClientRequestQueue requestQueue;
    private RSACryptoServiceProvider keyCryptor;

    private bool waitingApiName;
    private string serverApiVersion;

    private bool reconnect;
    private bool reconnecting;
    private DateTime lastReconnect;
    private DateTime lastPingRequest;

    private readonly object timerSync = new object();
    private Timer systemTimer;
    #endregion

    #region constructors
    /// <summary>
    /// Создает клиентское подключение к серверу.
    /// </summary>
    [SecurityCritical]
    public AsyncClient(string nick)
    {
      requestQueue = new ClientRequestQueue();
      keyCryptor = new RSACryptoServiceProvider(CryptorKeySize);

      waitingApiName = false;
      reconnecting = false;
      reconnect = true;
      Id = nick;
    }
    #endregion

    #region properties/events
    /// <summary>
    /// Взвращает значение, характеризующее подключен ли клиент к серверу.
    /// </summary>
    public bool IsConnected
    {
      [SecuritySafeCritical]
      get { return handler == null ? false : handler.Connected; }
    }

    /// <summary>
    /// Открытый ключ данного соединения.
    /// </summary>
    public RSAParameters OpenKey
    {
      [SecuritySafeCritical]
      get
      {
        ThrowIfDisposed();
        return keyCryptor.ExportParameters(false);
      }
    }

    /// <summary>
    /// Ассиметричный алгоритм шифрования, данного соединения.
    /// </summary>
    public RSACryptoServiceProvider KeyCryptor
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return keyCryptor;
      }
    }

    /// <summary>
    /// Версия API используемая на сервере.
    /// </summary>
    public string ServerApiVersion
    {
      [SecuritySafeCritical]
      get
      {
        ThrowIfDisposed();

        if (serverApiVersion == null)
          return string.Empty;

        return serverApiVersion;
      }
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
        throw new SocketException((int)SocketError.IsConnected);

      waitingApiName = true;
      hostAddress = serverAddress;
      systemTimer = new Timer(SystemTimerCallback, null, SystemTimerInterval, -1);

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

        Construct(handler, MaxReceivedDataSize);
        reconnecting = false;
      }
      catch (ObjectDisposedException) { return; }
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
    protected override void OnDataReceived(DataReceivedEventArgs e)
    {
      try
      {
        if (e.Error != null)
          throw e.Error;

        if (TrySetApi(e))
          return;

        var command = ClientModel.API.GetCommand(e.ReceivedData);
        var args = new ClientCommandArgs { Message = e.ReceivedData };

        requestQueue.Add(ClientId, command, args);
      }
      catch (Exception exc)
      {
        ClientModel.Notifier.AsyncError(new AsyncErrorEventArgs { Error = exc });
        ClientModel.Logger.Write(exc);
      }
    }

    [SecurityCritical]
    private bool TrySetApi(DataReceivedEventArgs e)
    {
      if (!waitingApiName)
        return false;

      serverApiVersion = Encoding.Unicode.GetString(e.ReceivedData);

      switch (serverApiVersion)
      {
        case StandardServerAPI.API:
          ClientModel.API = new StandardClientAPI();
          break;
      }

      if (ClientModel.API != null)
      {
        ClientModel.Notifier.Connected(new ConnectEventArgs { Error = null });
        waitingApiName = false;
      }
      else
        throw new ModelException(ErrorCode.APINotSupported, serverApiVersion);

      return true;
    }

    [SecuritySafeCritical]
    protected override void OnDataSended(DataSendedEventArgs args)
    {
      if (args.Error == null)
        return;

      ClientModel.Notifier.AsyncError(new AsyncErrorEventArgs { Error = args.Error });
      ClientModel.Logger.Write(args.Error);
    }

    [SecuritySafeCritical]
    protected override bool HandleSocketException(SocketException se)
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
    private void SystemTimerCallback(object state)
    {
      if (handler != null && IsConnected)
      {
        if ((DateTime.Now - lastPingRequest).TotalMilliseconds >= PingInterval && ClientModel.API != null)
        {
          ClientModel.API.PingRequest();
          lastPingRequest = DateTime.Now;
        }
      }

      if (reconnecting)
      {
        if ((DateTime.Now - lastReconnect).TotalMilliseconds >= ReconnectTimeInterval)
        {
          ClientModel.Notifier.ReceiveMessage(new ReceiveMessageEventArgs { Message = "Попытка соединения с сервером...", Type = MessageType.System });

          if (handler != null)
            handler.Close();

          Connect(hostAddress);

          lastReconnect = DateTime.Now;
        }
      }

      lock (timerSync)
        if (systemTimer != null)
          systemTimer.Change(SystemTimerInterval, -1);
    }
    #endregion

    #region IDisposable

    [SecuritySafeCritical]
    protected override void DisposeManagedResources()
    {
      base.DisposeManagedResources();

      if (requestQueue != null)
        requestQueue.Dispose();

      keyCryptor.Dispose();

      lock (timerSync)
      {
        if (systemTimer != null)
          systemTimer.Dispose();

        systemTimer = null;
      }
    }

    #endregion
  }
}
