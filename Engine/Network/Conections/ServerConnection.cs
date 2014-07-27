using Engine.Helpers;
using Engine.Model.Server;
using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Engine.Network.Connections
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
        return (IsRegistered) ? 0 : (int)(DateTime.Now - createTime).TotalMilliseconds;
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
        return id != null && !id.Contains(TempConnectionPrefix);
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
    /// <param name="id">Идентификатор соединения.</param>
    /// <param name="openKey">Открытый ключ соединения.</param>
    public void Register(string id, RSAParameters openKey)
    {
      ThrowIfDisposed();
      this.id = id;
      this.openKey = openKey;
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
