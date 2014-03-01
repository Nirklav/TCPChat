using Engine.Concrete;
using Engine.Concrete.Entities;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Engine.Abstract.Connections
{
  /// <summary>
  /// Базовый класс соединения, реализовывает прием и передачу данных.
  /// </summary>
  public abstract class Connection : IDisposable
  {
    #region consts
    private const int bufferSize = 4;
    #endregion

    #region protected fields
    protected int maxReceivedDataSize;
    protected byte[] buffer;
    protected Socket handler;
    protected MemoryStream receivedData;
    protected User info;
    #endregion

    #region constructors
    protected void Construct(Socket Handler, int MaxReceivedDataSize)
    {
      if (Handler == null)
        throw new ArgumentNullException();

      if (!Handler.Connected)
        throw new ArgumentException("Сокет должен быть соединен.");

      if (MaxReceivedDataSize <= 0)
        throw new ArgumentException("MaxReceivedDataSize должно быть больше 0.");

      handler = Handler;
      maxReceivedDataSize = MaxReceivedDataSize;
      buffer = new byte[bufferSize];
      receivedData = new MemoryStream();

      handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
    }
    #endregion

    #region properties
    /// <summary>
    /// Информация о соединении.
    /// </summary>
    public virtual User Info
    {
      get
      {
        ThrowIfDisposed();
        return info;
      }

      set
      {
        ThrowIfDisposed();
        info = value;
      }
    }

    /// <summary>
    /// Удаленная точка.
    /// </summary>
    public virtual IPEndPoint RemotePoint
    {
      get
      {
        ThrowIfDisposed();
        return (IPEndPoint)handler.RemoteEndPoint;
      }
    }

    /// <summary>
    /// Локальная точка.
    /// </summary>
    public virtual IPEndPoint LocalPoint
    {
      get
      {
        ThrowIfDisposed();
        return (IPEndPoint)handler.LocalEndPoint;
      }
    }
    #endregion

    #region public methods
    /// <summary>
    /// Отправляет команду.
    /// </summary>
    /// <param name="id">Индетификатор команды.</param>
    /// <param name="messageContent">Параметр команды.</param>
    public virtual void SendMessage(ushort id, object messageContent)
    {
      ThrowIfDisposed();

      if (!handler.Connected)
        throw new InvalidOperationException("not connected");

      try
      {
        MemoryStream messageStream = new MemoryStream();

        messageStream.Write(BitConverter.GetBytes(0), 0, sizeof(int));
        messageStream.Write(BitConverter.GetBytes(id), 0, sizeof(ushort));

        if (messageContent != null)
        {
          BinaryFormatter formatter = new BinaryFormatter();
          formatter.Serialize(messageStream, messageContent);
        }

        byte[] messageToSend = messageStream.ToArray();
        int messageToSendSize = (int)messageStream.Length;
        Buffer.BlockCopy(BitConverter.GetBytes(messageToSendSize), 0, messageToSend, 0, sizeof(int));
        handler.BeginSend(messageToSend, 0, messageToSend.Length, SocketFlags.None, SendCallback, null);
      }
      catch (SocketException se)
      {
        if (!HandleSocketException(se))
          throw se;
      }
    }
    #endregion

    #region private callback methods
    private void ReceiveCallback(IAsyncResult result)
    {
      try
      {
        int BytesRead = handler.EndReceive(result);

        if (BytesRead > 0)
        {
          OnPackageReceive();

          receivedData.Write(buffer, 0, BytesRead);

          if (DataIsReceived())
          {
            OnDataReceived(new DataReceivedEventArgs() { ReceivedData = GetData(), Error = null });
          }
          else
          {
            if (GetSizeReceivingData() > maxReceivedDataSize)
            {
              throw new ReceivedDataException();
            }
          }
        }

        handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
      }
      catch (ObjectDisposedException) { return; }
      catch (SocketException se)
      {
        if (!HandleSocketException(se))
          OnDataReceived(new DataReceivedEventArgs() { ReceivedData = null, Error = se });
      }
      catch (Exception e)
      {
        OnDataReceived(new DataReceivedEventArgs() { ReceivedData = null, Error = e });
      }
    }

    private void SendCallback(IAsyncResult result)
    {
      try
      {
        int SendedDataSize = handler.EndSend(result);
        OnDataSended(new DataSendedEventArgs() { SendedDataCount = SendedDataSize, Error = null });
      }
      catch (ObjectDisposedException) { return; }
      catch (SocketException se)
      {
        if (!HandleSocketException(se))
          OnDataSended(new DataSendedEventArgs() { Error = se });
      }
      catch (Exception e)
      {
        OnDataSended(new DataSendedEventArgs() { Error = e });
      }
    }
    #endregion

    #region protected virtual/abstract methods
    /// <summary>
    /// Происходит когда получено полное сообщение.
    /// </summary>
    /// <param name="args">Инормация о данных, и данные.</param>
    protected abstract void OnDataReceived(DataReceivedEventArgs args);

    /// <summary>
    /// Происходит при отправке данных. Или при возниконовении ошибки произошедшей во время передачи данных.
    /// </summary>
    /// <param name="args">Информация о отправленных данных.</param>
    protected abstract void OnDataSended(DataSendedEventArgs args);

    /// <summary>
    /// Происходит при получении пакета данных.
    /// </summary>
    protected virtual void OnPackageReceive() { }

    /// <summary>
    /// Происходит при ловле классом SocketException. Без переопределение возращает всегда false.
    /// </summary>
    /// <param name="se">Словленое исключение.</param>
    /// <returns>Вовзращает значение говорящее о том, нужно ли дальше выкидывать исключение, или оно обработано. true - обработано. false - не обработано.</returns>
    protected virtual bool HandleSocketException(SocketException se)
    {
      return false;
    }
    #endregion

    #region private methods
    private byte[] GetData()
    {
      if (!DataIsReceived())
        return null;

      byte[] memoryStreamBuffer = receivedData.GetBuffer();

      byte[] resultData = new byte[GetSizeReceivingData() - sizeof(int)];
      Buffer.BlockCopy(memoryStreamBuffer, sizeof(int), resultData, 0, resultData.Length);

      int restDataSize = (int)(receivedData.Position - GetSizeReceivingData());
      int sizeOfReceivingData = GetSizeReceivingData();

      receivedData.Seek(0, SeekOrigin.Begin);

      if (restDataSize > 0)
        receivedData.Write(memoryStreamBuffer, sizeOfReceivingData, restDataSize);

      if (DataIsReceived())
        OnDataReceived(new DataReceivedEventArgs() { ReceivedData = GetData(), Error = null });

      return resultData;
    }

    private bool DataIsReceived()
    {
      int receivingDataSize = GetSizeReceivingData();

      if (receivingDataSize == -1)
        return false;

      if (receivingDataSize > receivedData.Position)
        return false;

      return true;
    }

    private int GetSizeReceivingData()
    {
      if (receivedData.Length < sizeof(int))
        return -1;

      int DataSize = BitConverter.ToInt32(receivedData.GetBuffer(), 0);

      return DataSize;
    }

    protected virtual void SendMessage(byte[] data)
    {
      try
      {
        MemoryStream messageStream = new MemoryStream();
        messageStream.Write(BitConverter.GetBytes(data.Length + sizeof(int)), 0, sizeof(int));
        messageStream.Write(data, 0, data.Length);

        byte[] message = messageStream.ToArray();
        messageStream.Dispose();

        handler.BeginSend(message, 0, message.Length, SocketFlags.None, SendCallback, null);
      }
      catch (SocketException se)
      {
        if (!HandleSocketException(se))
          throw se;
      }
    }
    #endregion

    #region IDisposable
    bool disposed = false;

    protected void ThrowIfDisposed()
    {
      if (disposed)
        throw new ObjectDisposedException("Object disposed");
    }

    protected virtual void ReleaseResource()
    {
      if (disposed)
        return;

      if (handler != null)
      {
        if (handler.Connected)
          handler.Disconnect(false);

        handler.Close();
      }

      if (receivedData != null)
        receivedData.Dispose();

      disposed = true;
    }

    public void Dispose()
    {
      ReleaseResource();
    }
    #endregion

    #region utils
    /// <summary>
    /// Проверяет TCP порт на занятость.
    /// </summary>
    /// <param name="port">Порт который необходимо проверить.</param>
    /// <returns>Возвращает true если порт свободный.</returns>
    public static bool TCPPortIsAvailable(int port)
    {
      if (port < 0 || port > ushort.MaxValue)
        return false;

      IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
      TcpConnectionInformation[] tcpConnectionsInfo = ipGlobalProperties.GetActiveTcpConnections();
      IPEndPoint[] listenersPoints = ipGlobalProperties.GetActiveTcpListeners();

      foreach (TcpConnectionInformation tcpi in tcpConnectionsInfo)
      {
        if (tcpi.LocalEndPoint.Port == port)
          return false;
      }

      foreach (IPEndPoint ep in listenersPoints)
      {
        if (ep.Port == port)
          return false;
      }

      return true;
    }

    /// <summary>
    /// Узнает IP адрес данного компьютера.
    /// </summary>
    /// <param name="type">Тип адреса.</param>
    /// <returns>IP адрес данного компьютера.</returns>
    public static IPAddress GetIPAddress(AddressFamily type)
    {
      IPAddress address = null;
      foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        if (ip.AddressFamily == type && !ip.IsIPv6LinkLocal && !ip.IsIPv6SiteLocal && !ip.IsIPv6Multicast)
          address = ip;

      return address;
    }
    #endregion
  }
}
