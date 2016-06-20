using Engine.Exceptions;
using Engine.Helpers;
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
    [SecurityCritical] private ECDiffieHellmanCng diffieHellman;
    [SecurityCritical] private byte[] key;
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

      MemoryStream stream = null;
      try
      {
        var encrypt = key != null;

        stream = new MemoryStream();
        stream.Write(BitConverter.GetBytes(0), 0, sizeof(int));
        stream.Write(BitConverter.GetBytes(encrypt), 0, sizeof(bool));

        if (!encrypt)
          Serializer.Serialize(stream, package);
        else
        {
          using (var crypter = new Crypter())
          {
            crypter.SetKey(key);
            crypter.Encrypt(package, stream);
          }
        }

        var message = stream.ToArray();
        var lengthBlob = BitConverter.GetBytes(message.Length);
        Buffer.BlockCopy(lengthBlob, 0, message, 0, lengthBlob.Length);
        handler.BeginSend(message, 0, message.Length, SocketFlags.None, OnSend, null);
      }
      catch (SocketException se)
      {
        if (!OnSocketException(se))
          throw;
      }
      finally
      {
        if (stream != null)
          stream.Dispose();
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

          if (GetReceivingPackageSize() > MaxReceivedDataSize)
            throw new ModelException(ErrorCode.LargeReceivedData);

          while (IsPackageReceived())
          {
            var package = GetReceivedPackage();

            switch (package.Id)
            {
              case RemoteInfoId:
                if (!connectionInfoSent)
                  SendInfo();

                SetRemoteInfo(package);
                break;
              default:
                OnPackageReceived(new PackageReceivedEventArgs(package));
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
      key = diffieHellman.DeriveKeyMaterial(publicKey);

      OnInfoReceived(remoteInfo);
    }

    [SecurityCritical]
    private bool IsPackageReceived()
    {
      var size = GetReceivingPackageSize();
      if (size == -1)
        return false;

      if (size > received.Position)
        return false;

      return true;
    }

    [SecurityCritical]
    private int GetReceivingPackageSize()
    {
      if (received.Position < HeadSize)
        return -1;

      return BitConverter.ToInt32(received.GetBuffer(), LengthHead);
    }

    [SecurityCritical]
    private bool IsReceivingPackageEncrypted()
    {
      if (received.Position < HeadSize)
        return false;

      return BitConverter.ToBoolean(received.GetBuffer(), EncryptionHead);
    }

    [SecurityCritical]
    private IPackage GetReceivedPackage()
    {
      if (!IsPackageReceived())
        throw new InvalidOperationException("Package not received yet");

      var receivingSize = GetReceivingPackageSize();
      var restDataSize = (int)(received.Position - receivingSize);

      received.Position = HeadSize;

      IPackage result;
      if (!IsReceivingPackageEncrypted())
        result = Serializer.Deserialize<IPackage>(received);
      else
      {
        if (key == null)
          throw new InvalidOperationException("Key not set yet");

        var packageBlob = new byte[receivingSize - HeadSize];
        received.Read(packageBlob, 0, packageBlob.Length);

        using (var crypter = new Crypter())
        {
          crypter.SetKey(key);
          result = crypter.Decrypt<IPackage>(packageBlob);
        }
      }

      received.Position = 0;
      if (restDataSize > 0)
      {
        var dataBuffer = received.GetBuffer();
        received.Write(dataBuffer, receivingSize, restDataSize);
      }

      return result;
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

      key = null;
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
