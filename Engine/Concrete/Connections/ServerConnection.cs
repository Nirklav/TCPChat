using Engine.Abstract.Connections;
using Engine.Concrete.Entities;
using Engine.Concrete.Helpers;
using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Engine.Concrete.Connections
{
  /// <summary>
  /// Серверное соединение с клиентом.
  /// </summary>
  public sealed class ServerConnection :
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
    private bool isRegistered;
    private RSAParameters openKey;
    private DateTime lastActivity;
    private DateTime createTime;
    private Logger logger;
    private EventHandler<DataReceivedEventArgs> dataReceivedCallback;
    #endregion

    #region constructors
    /// <summary>
    /// Создает серверное подключение.
    /// </summary>
    /// <param name="handler">Подключенный к клиенту сокет.</param>
    /// <param name="MaxReceivedDataSize">Максимальныйц размер сообщения получаемый от пользователя.</param>
    /// <param name="ConnectionLogger">Логгер.</param>
    /// <param name="DataReceivedCallback">Функция оповещающая о полученнии сообщения, данным соединением.</param>
    public ServerConnection(Socket handler, int MaxReceivedDataSize, Logger ConnectionLogger, EventHandler<DataReceivedEventArgs> DataReceivedCallback)
    {
      Construct(handler, MaxReceivedDataSize);

      if (ConnectionLogger == null)
        throw new ArgumentNullException();

      if (DataReceivedCallback == null)
        throw new ArgumentNullException();

      logger = ConnectionLogger;
      dataReceivedCallback = DataReceivedCallback;

      lastActivity = DateTime.Now;
      createTime = DateTime.Now;

      isRegistered = false;
    }
    #endregion

    #region properties
    /// <summary>
    /// Интервал нективности подключения.
    /// </summary>
    public int IntervalOfSilence
    {
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
      get
      {
        ThrowIfDisposed();
        return (isRegistered) ? 0 : (int)(DateTime.Now - createTime).TotalMilliseconds;
      }
    }

    /// <summary>
    /// Возвращает значение характеризующее зарегистрированно соединение или нет.
    /// </summary>
    public bool IsRegistered
    {
      get
      {
        ThrowIfDisposed();
        return isRegistered;
      }
    }

    /// <summary>
    /// Откртый ключ подключения.
    /// </summary>
    public RSAParameters OpenKey
    {
      get
      {
        ThrowIfDisposed();
        return openKey;
      }

      set
      {
        ThrowIfDisposed();
        openKey = value;
      }
    }
    #endregion

    #region public methods
    /// <summary>
    /// Отправляет сообщение с именем API, которое использует сервер.
    /// </summary>
    /// <param name="APIName">Название API.</param>
    public void SendAPIName(string APIName)
    {
      ThrowIfDisposed();
      SendMessage(Encoding.Unicode.GetBytes(APIName));
    }

    /// <summary>
    /// Регистрирует данное соединение.
    /// </summary>
    /// <param name="Nick"></param>
    public void Register(string nick)
    {
      ThrowIfDisposed();
      info = new User(nick);
      isRegistered = true;
    }
    #endregion

    #region override methods
    protected override void OnPackageReceive()
    {
      lastActivity = DateTime.Now;
    }

    protected override void OnDataReceived(DataReceivedEventArgs args)
    {
      if (args.Error != null)
      {
        if (args.Error is SocketException)
        {
          SocketException se = (SocketException)args.Error;
          if (se.SocketErrorCode == SocketError.ConnectionReset)
            return;
        }

        logger.Write(args.Error);
        return;
      }

      EventHandler<DataReceivedEventArgs> temp = Interlocked.CompareExchange<EventHandler<DataReceivedEventArgs>>(ref dataReceivedCallback, null, null);

      if (temp != null)
        temp(this, args);
    }

    protected override void OnDataSended(DataSendedEventArgs args)
    {
      if (args.Error != null)
        logger.Write(args.Error);

      lastActivity = DateTime.Now;
    }
    #endregion
  }
}
