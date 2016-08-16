using Engine.Exceptions;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;

namespace Engine.Network.Connections
{
  /// <summary>
  /// Базовый класс соединения, реализовывает прием и передачу данных.
  /// </summary>
  public abstract class Connection :
    MarshalByRefObject,
    IDisposable
  {
    #region consts
    private const long RemoteInfoId = 1;

    private const int HeadSize = sizeof(int) + sizeof(bool);
    private const int LengthHead = 0;
    private const int EncryptionHead = sizeof(int);

    private const int KeySize = 256;
    private const int BufferSize = 4096;
    private const int MaxReceivedDataSize = 1024 * 1024;
    public const string TempConnectionPrefix = "tempId_";
    #endregion

    #region fields
    [SecurityCritical] protected string id;
    [SecurityCritical] protected ConnectionInfo remoteInfo;
    [SecurityCritical] private bool connectionInfoSent;
    [SecurityCritical] protected byte[] buffer;
    [SecurityCritical] protected Socket handler;
    [SecurityCritical] protected MemoryStream received;
    [SecurityCritical] protected Packer packer;
    [SecurityCritical] private ECDiffieHellmanCng diffieHellman;
    [SecurityCritical] protected volatile bool disposed;
    #endregion

    #region constructors
    [SecurityCritical]
    protected void Construct(Socket socket)
    {
      if (socket == null)
        throw new ArgumentNullException("socket");

      if (!socket.Connected)
        throw new ArgumentException("Socket should be connected.");

      handler = socket;
      buffer = new byte[BufferSize];
      received = new MemoryStream();
      packer = new Packer();

      diffieHellman = new ECDiffieHellmanCng(KeySize);
      diffieHellman.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
      diffieHellman.HashAlgorithm = CngAlgorithm.Sha256;

      handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, null);
    }
    #endregion

    #region properties
    /// <summary>
    /// Идентификатор соединения.
    /// </summary>
    public string Id
    {
      [SecurityCritical]
      get { return id; }
      [SecurityCritical]
      set
      {
        ThrowIfDisposed();
        id = value;
      }
    }

    /// <summary>
    /// Удаленная точка.
    /// </summary>
    public IPEndPoint RemotePoint
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return (IPEndPoint)handler.RemoteEndPoint;
      }
    }

    /// <summary>
    /// Локальная точка.
    /// </summary>
    public IPEndPoint LocalPoint
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return (IPEndPoint)handler.LocalEndPoint;
      }
    }
    #endregion

    #region public methods
    /// <summary>
    /// Отправляет пакет.
    /// </summary>
    /// <param name="id">Индетификатор пакета.</param>
    [SecuritySafeCritical]
    public void SendMessage(long id)
    {
      SendMessage(new Package(id));
    }

    /// <summary>
    /// Отправляет пакет.
    /// </summary>
    /// <param name="id">Индетификатор пакета.</param>
    /// <param name="content">Данные пакета.</param>
    [SecuritySafeCritical]
    public void SendMessage<T>(long id, T content)
    {
      SendMessage(new Package<T>(id, content));
    }

    /// <summary>
    /// Отправляет пакет.
    /// </summary>
    /// <param name="package">Отправляемый пакет.</param>
    [SecurityCritical]
    public void SendMessage(IPackage package)
    {
      ThrowIfDisposed();

      if (handler == null)
        throw new InvalidOperationException("Socket not set");

      if (!handler.Connected)
        throw new InvalidOperationException("Not connected");

      using (var packed = packer.Pack(package))
      {
        try
        {
          handler.BeginSend(packed.Data, 0, packed.Length, SocketFlags.None, OnSend, null);
        }
        catch (SocketException se)
        {
          if (!OnSocketException(se))
            throw;
        }
      }
    }

    /// <summary>
    /// Отправляет информацию о соединении.
    /// </summary>
    [SecurityCritical]
    public void SendInfo()
    {
      if (connectionInfoSent)
        throw new InvalidOperationException("Connection info already sent");

      connectionInfoSent = true;

      var info = CreateConnectionInfo();
      info.PublicKey = diffieHellman.PublicKey.ToByteArray();
      SendMessage(RemoteInfoId, info);
    }

    /// <summary>
    /// Инциирует отключение соединения.
    /// </summary>
    [SecurityCritical]
    public void Disconnect()
    {
      ThrowIfDisposed();

      if (handler == null)
        throw new InvalidOperationException("Socket not set");

      if (!handler.Connected)
        throw new InvalidOperationException("not connected");

      handler.BeginDisconnect(true, OnDisconnect, null);
    }
    #endregion

    #region callback methods
    [SecurityCritical]
    private void OnReceive(IAsyncResult result)
    {
      if (disposed)
        return;

      try
      {
        var bytesRead = handler.EndReceive(result);
        if (bytesRead > 0)
        {
          OnPackagePartReceived();

          received.Write(buffer, 0, bytesRead);

          var size = packer.GetPackageSize(received);
          if (size > MaxReceivedDataSize)
            throw new ModelException(ErrorCode.LargeReceivedData);

          while (packer.IsPackageReceived(received))
          {
            var unpacked = packer.Unpack<IPackage>(received);

            var length = (int) received.Length;

            received.Position = 0;
            received.SetLength(0);

            var restDataSize = length - size;
            if (restDataSize > 0)
            {
              var dataBuffer = received.GetBuffer();
              received.Write(dataBuffer, size, restDataSize);
            }

            switch (unpacked.Package.Id)
            {
              case RemoteInfoId:
                if (!connectionInfoSent)
                  SendInfo();

                SetRemoteInfo(unpacked.Package);
                unpacked.Dispose();
                break;
              default:
                OnPackageReceived(new PackageReceivedEventArgs(unpacked));
                break;
            }
          }
        }

        handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, null);
      }
      catch (SocketException se)
      {
        if (!OnSocketException(se))
          OnPackageReceived(new PackageReceivedEventArgs(se));
      }
      catch (Exception e)
      {
        OnPackageReceived(new PackageReceivedEventArgs(e));
      }
    }

    [SecurityCritical]
    private void OnSend(IAsyncResult result)
    {
      if (disposed)
        return;

      try
      {
        var size = handler.EndSend(result);
        OnPackageSent(new PackageSendedEventArgs(size));
      }
      catch (SocketException se)
      {
        if (!OnSocketException(se))
          OnPackageSent(new PackageSendedEventArgs(se));
      }
      catch (Exception e)
      {
        OnPackageSent(new PackageSendedEventArgs(e));
      }
    }

    [SecurityCritical]
    private void OnDisconnect(IAsyncResult result)
    {
      if (disposed)
        return;

      try
      {
        handler.EndDisconnect(result);
        OnDisconnected(null);
      }
      catch (SocketException se)
      {
        if (!OnSocketException(se))
          OnDisconnected(se);
      }
      catch (Exception e)
      {
        OnDisconnected(e);
      }
    }
    #endregion

    #region protected virtual/abstract methods
    /// <summary>
    /// Создает объект содержащий информацию о содединении.
    /// </summary>
    /// <returns>Объект содержащий информацию о содединении.</returns>
    [SecuritySafeCritical]
    protected virtual ConnectionInfo CreateConnectionInfo()
    {
      return new ConnectionInfo();
    }

    /// <summary>
    /// Происходит при получении информации о удаленном соединении.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void OnInfoReceived(ConnectionInfo info) { }

    /// <summary>
    /// Происходит когда получено полное сообщение.
    /// </summary>
    /// <param name="args">Инормация о данных, и данные.</param>
    [SecuritySafeCritical]
    protected abstract void OnPackageReceived(PackageReceivedEventArgs args);

    /// <summary>
    /// Происходит при отправке данных. Или при возниконовении ошибки произошедшей во время передачи данных.
    /// </summary>
    /// <param name="args">Информация о отправленных данных.</param>
    [SecuritySafeCritical]
    protected abstract void OnPackageSent(PackageSendedEventArgs args);

    /// <summary>
    /// Происходит при получении части данных.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void OnPackagePartReceived() { }

    /// <summary>
    /// Происходит при отсоединении.
    /// </summary>
    /// <param name="e">Ошибка которая могла возникнуть в процессе отсоединения.</param>
    [SecuritySafeCritical]
    protected virtual void OnDisconnected(Exception e) { }

    /// <summary>
    /// Происходит при SocketException. Без переопределение возращает всегда false.
    /// </summary>
    /// <param name="se">Словленое исключение.</param>
    /// <returns>Вовзращает значение говорящее о том, нужно ли дальше выкидывать исключение или оно обработано. true - обработано. false - не обработано.</returns>
    [SecuritySafeCritical]
    protected virtual bool OnSocketException(SocketException se)
    {
      return false;
    }
    #endregion

    #region private methods
    [SecurityCritical]
    private void SetRemoteInfo(IPackage package)
    {
      var remoteInfoPack = package as IPackage<ConnectionInfo>;
      if (remoteInfoPack == null)
        throw new ArgumentException("package isn't IPackage<ConnectionInfo>");

      // Set key
      remoteInfo = remoteInfoPack.Content;
      var publicKey = CngKey.Import(remoteInfo.PublicKey, CngKeyBlobFormat.EccPublicBlob);
      packer.SetKey(diffieHellman.DeriveKeyMaterial(publicKey));

      OnInfoReceived(remoteInfo);
    }
    #endregion

    #region IDisposable
    [SecurityCritical]
    protected void ThrowIfDisposed()
    {
      if (disposed)
        throw new ObjectDisposedException("Object disposed");
    }

    /// <summary>
    /// Очишает соединение. После вызова класс может быть переиспользован.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void Clean()
    {
      if (handler != null)
      {
        if (handler.Connected)
        {
          handler.Shutdown(SocketShutdown.Both);
          handler.Disconnect(false);
          OnDisconnected(null);
        }

        handler.Dispose();
        handler = null;
      }

      if (diffieHellman != null)
      {
        diffieHellman.Dispose();
        diffieHellman = null;
      }

      if (received != null)
      {
        received.Dispose();
        received = null;
      }

      connectionInfoSent = false;
      remoteInfo = null;
    }

    /// <summary>
    /// Освобождает управляемые ресурсы соедиенения.
    /// Не может быть переиспользован после вызова.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void DisposeManagedResources()
    {
      Clean();
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (disposed)
        return;

      disposed = true;
      DisposeManagedResources();
    }
    #endregion

    #region utils
    /// <summary>
    /// Проверяет TCP порт на занятость.
    /// </summary>
    /// <param name="port">Порт который необходимо проверить.</param>
    /// <returns>Возвращает true если порт свободный.</returns>
    [SecuritySafeCritical]
    public static bool TcpPortIsAvailable(int port)
    {
      if (port < 0 || port > ushort.MaxValue)
        return false;

      var properties = IPGlobalProperties.GetIPGlobalProperties();
      var connections = properties.GetActiveTcpConnections();
      var listeners = properties.GetActiveTcpListeners();

      foreach (var connection in connections)
        if (connection.LocalEndPoint.Port == port)
          return false;

      foreach (var listener in listeners)
        if (listener.Port == port)
          return false;

      return true;
    }

    /// <summary>
    /// Узнает IP адрес данного компьютера.
    /// </summary>
    /// <param name="type">Тип адреса.</param>
    /// <returns>IP адрес данного компьютера.</returns>
    [SecuritySafeCritical]
    public static IPAddress GetIpAddress(AddressFamily type)
    {
      var hostName = Dns.GetHostName();

      foreach (var ip in Dns.GetHostAddresses(hostName))
      {
        if (ip.AddressFamily == type 
          && !IPAddress.IsLoopback(ip) 
          && !ip.IsIPv6LinkLocal 
          && !ip.IsIPv6SiteLocal 
          && !ip.IsIPv6Multicast)
          return ip;
      }

      return null;
    }
    #endregion
  }
}
