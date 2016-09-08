using Engine.Model.Server;
using System;
using System.Net.Sockets;
using System.Security;
using System.Threading;

namespace Engine.Network
{
  /// <summary>
  /// Серверное соединение с клиентом.
  /// </summary>
  sealed class ServerConnection :
    Connection
  {
    #region consts
    /// <summary>
    /// Время неактивности соединения, после прошествия которого соединение будет закрыто.
    /// </summary>
    public const int ConnectionTimeOut = 7 * 1000;

    /// <summary>
    /// Время ожидания регистрации. После того как данное время закончится соединение будет закрыто.
    /// </summary>
    public const int UnregisteredTimeOut = 60 * 1000;
    #endregion

    #region private field
    [SecurityCritical] private string _serverApiName;
    [SecurityCritical] private DateTime _lastActivity;
    [SecurityCritical] private DateTime _createTime;
    [SecurityCritical] private EventHandler<PackageReceivedEventArgs> _receivedCallback;
    #endregion

    #region constructors
    /// <summary>
    /// Создает серверное подключение.
    /// </summary>
    /// <param name="handler">Подключенный к клиенту сокет.</param>
    /// <param name="apiName">Текущая версия Api.</param>
    /// <param name="receivedCallback">Функция обратного вызова, для полученных данных.</param>
    [SecurityCritical]
    public ServerConnection(Socket handler, string apiName, EventHandler<PackageReceivedEventArgs> receivedCallback)
    {
      if (receivedCallback == null)
        throw new ArgumentNullException();

      Construct(handler);

      _serverApiName = apiName;
      _receivedCallback = receivedCallback;
      _lastActivity = DateTime.UtcNow;
      _createTime = DateTime.UtcNow;
    }
    #endregion

    #region properties
    /// <summary>
    /// Интервал нективности подключения.
    /// </summary>
    public int IntervalOfSilence
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return (int)(DateTime.UtcNow - _lastActivity).TotalMilliseconds;
      }
    }

    /// <summary>
    /// Интервал незарегистрированности соединения.
    /// </summary>
    public int UnregisteredTimeInterval
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return (IsRegistered) ? 0 : (int)(DateTime.UtcNow - _createTime).TotalMilliseconds;
      }
    }

    /// <summary>
    /// Возвращает значение характеризующее зарегистрированно соединение или нет.
    /// </summary>
    public bool IsRegistered
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return _id != null && !_id.Contains(TempConnectionPrefix);
      }
    }
    #endregion

    #region public methods
    /// <summary>
    /// Регистрирует данное соединение.
    /// </summary>
    /// <param name="newId">Идентификатор соединения.</param>
    [SecurityCritical]
    public void Register(string newId)
    {
      ThrowIfDisposed();
      _id = newId;
    }
    #endregion

    #region override methods
    [SecuritySafeCritical]
    protected override ConnectionInfo CreateConnectionInfo()
    {
      var info = new ServerConnectionInfo();
      info.ApiName = _serverApiName;
      return info;
    }

    [SecuritySafeCritical]
    protected override void OnPackagePartReceived()
    {
      _lastActivity = DateTime.UtcNow;
    }

    [SecuritySafeCritical]
    protected override void OnPackageReceived(PackageReceivedEventArgs args)
    {
      if (args.Exception != null)
      {
        var se = args.Exception as SocketException;
        if (se != null && se.SocketErrorCode == SocketError.ConnectionReset)
          return;

        ServerModel.Logger.Write(args.Exception);
        return;
      }

      var temp = Interlocked.CompareExchange(ref _receivedCallback, null, null);
      if (temp != null)
        temp(this, args);
    }

    [SecuritySafeCritical]
    protected override void OnPackageSent(PackageSendedEventArgs args)
    {
      if (args.Exception != null)
        ServerModel.Logger.Write(args.Exception);
      else
        _lastActivity = DateTime.UtcNow;
    }
    #endregion
  }
}
