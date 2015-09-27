using Engine.Model.Server;
using System;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Engine.Network.Connections
{
  /// <summary>
  /// Серверное соединение с клиентом.
  /// </summary>
  [SecuritySafeCritical]
  sealed class ServerConnection :
    Connection
  {
    #region public const
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
    private RSAParameters openKey;
    private DateTime lastActivity;
    private DateTime createTime;
    private EventHandler<DataReceivedEventArgs> dataReceivedCallback;
    #endregion

    #region constructors
    /// <summary>
    /// Создает серверное подключение.
    /// </summary>
    /// <param name="handler">Подключенный к клиенту сокет.</param>
    /// <param name="maxReceivedDataSize">Максимальныйц размер сообщения получаемый от пользователя.</param>
    /// <param name="ConnectionLogger">Логгер.</param>
    /// <param name="receivedCallback">Функция оповещающая о полученнии сообщения, данным соединением.</param>
    [SecurityCritical]
    public ServerConnection(Socket handler, int maxReceivedDataSize, EventHandler<DataReceivedEventArgs> receivedCallback)
    {
      Construct(handler, maxReceivedDataSize);

      if (receivedCallback == null)
        throw new ArgumentNullException();

      dataReceivedCallback = receivedCallback;

      lastActivity = DateTime.Now;
      createTime = DateTime.Now;
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
        return (int)(DateTime.Now - lastActivity).TotalMilliseconds;
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
        return (IsRegistered) ? 0 : (int)(DateTime.Now - createTime).TotalMilliseconds;
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
        return id != null && !id.Contains(TempConnectionPrefix);
      }
    }

    /// <summary>
    /// Откртый ключ подключения.
    /// </summary>
    public RSAParameters OpenKey
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return openKey;
      }
    }
    #endregion

    #region public methods
    /// <summary>
    /// Отправляет сообщение с именем API, которое использует сервер.
    /// </summary>
    /// <param name="apiName">Название API.</param>
    [SecurityCritical]
    public void SendApiName(string apiName)
    {
      ThrowIfDisposed();
      SendMessage(Encoding.Unicode.GetBytes(apiName));
    }

    /// <summary>
    /// Регистрирует данное соединение.
    /// </summary>
    /// <param name="id">Идентификатор соединения.</param>
    /// <param name="openKey">Открытый ключ соединения.</param>
    [SecurityCritical]
    public void Register(string id, RSAParameters openKey)
    {
      ThrowIfDisposed();
      this.id = id;
      this.openKey = openKey;
    }
    #endregion

    #region override methods
    [SecuritySafeCritical]
    protected override void OnPackageReceived()
    {
      lastActivity = DateTime.Now;
    }

    [SecuritySafeCritical]
    protected override void OnDataReceived(DataReceivedEventArgs args)
    {
      if (args.Error != null)
      {
        SocketException se = args.Error as SocketException;
        if (se != null && se.SocketErrorCode == SocketError.ConnectionReset)
          return;

        ServerModel.Logger.Write(args.Error);
        return;
      }

      var temp = Interlocked.CompareExchange(ref dataReceivedCallback, null, null);

      if (temp != null)
        temp(this, args);
    }

    [SecuritySafeCritical]
    protected override void OnDataSended(DataSendedEventArgs args)
    {
      if (args.Error != null)
        ServerModel.Logger.Write(args.Error);
      else
        lastActivity = DateTime.Now;
    }
    #endregion
  }
}
