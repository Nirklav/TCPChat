using Engine.Exceptions;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;

namespace Engine.Network
{
  /// <summary>
  /// Base connection.
  /// </summary>
  public abstract class Connection :
    MarshalByRefObject,
    IDisposable
  {
    #region consts
    private const long RemoteInfoId = 1;

    private const int KeySize = 256;
    private const int BufferSize = 4096;
    private const int MaxReceivedDataSize = 1024 * 1024;
    public const string TempConnectionPrefix = "tempId_";
    #endregion

    #region fields
    [SecurityCritical] protected string _id;
    [SecurityCritical] protected ConnectionInfo _remoteInfo;
    [SecurityCritical] private bool _connectionInfoSent;
    [SecurityCritical] protected byte[] _buffer;
    [SecurityCritical] protected Socket _handler;
    [SecurityCritical] protected MemoryStream _received;
    [SecurityCritical] protected Packer _packer;
    [SecurityCritical] private ECDiffieHellmanCng _diffieHellman;
    [SecurityCritical] protected volatile bool _disposed;
    #endregion

    #region constructors
    [SecurityCritical]
    protected void Construct(Socket handler)
    {
      if (handler == null)
        throw new ArgumentNullException("socket");

      if (!handler.Connected)
        throw new ArgumentException("Socket should be connected.");

      _handler = handler;
      _buffer = new byte[BufferSize];
      _received = new MemoryStream();
      _packer = new Packer();

      _diffieHellman = new ECDiffieHellmanCng(KeySize);
      _diffieHellman.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
      _diffieHellman.HashAlgorithm = CngAlgorithm.Sha256;

      _handler.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnReceive, null);
    }
    #endregion

    #region properties
    /// <summary>
    /// Connection id.
    /// </summary>
    public string Id
    {
      [SecurityCritical]
      get { return _id; }
      [SecurityCritical]
      set
      {
        ThrowIfDisposed();
        _id = value;
      }
    }

    /// <summary>
    /// Remote address.
    /// </summary>
    public IPEndPoint RemotePoint
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return (IPEndPoint)_handler.RemoteEndPoint;
      }
    }

    /// <summary>
    /// Local address.
    /// </summary>
    public IPEndPoint LocalPoint
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return (IPEndPoint)_handler.LocalEndPoint;
      }
    }
    #endregion

    #region public methods
    /// <summary>
    /// Send package.
    /// </summary>
    /// <param name="id">Package id. (Command.Id)</param>
    [SecuritySafeCritical]
    public void SendMessage(long id)
    {
      SendMessage(new Package(id));
    }

    /// <summary>
    /// Send package.
    /// </summary>
    /// <param name="id">Package id. (Command.Id)</param>
    /// <param name="content">Package content.</param>
    [SecuritySafeCritical]
    public void SendMessage<T>(long id, T content)
    {
      SendMessage(new Package<T>(id, content));
    }

    /// <summary>
    /// Send package.
    /// </summary>
    /// <param name="package">Package.</param>
    [SecurityCritical]
    public void SendMessage(IPackage package)
    {
      ThrowIfDisposed();

      if (_handler == null)
        throw new InvalidOperationException("Socket not set");

      if (!_handler.Connected)
        throw new InvalidOperationException("Not connected");

      using (var packed = _packer.Pack(package))
      {
        try
        {
          _handler.BeginSend(packed.Data, 0, packed.Length, SocketFlags.None, OnSend, null);
        }
        catch (SocketException se)
        {
          if (!OnSocketException(se))
            throw;
        }
      }
    }

    /// <summary>
    /// Send connection info.
    /// </summary>
    [SecurityCritical]
    public void SendInfo()
    {
      if (_connectionInfoSent)
        throw new InvalidOperationException("Connection info already sent");

      _connectionInfoSent = true;

      var info = CreateConnectionInfo();
      info.PublicKey = _diffieHellman.PublicKey.ToByteArray();
      SendMessage(RemoteInfoId, info);
    }

    /// <summary>
    /// Start disconnect from remote.
    /// </summary>
    [SecurityCritical]
    public void Disconnect()
    {
      ThrowIfDisposed();

      if (_handler == null)
        throw new InvalidOperationException("Socket not set");

      if (!_handler.Connected)
        throw new InvalidOperationException("not connected");

      _handler.BeginDisconnect(true, OnDisconnect, null);
    }
    #endregion

    #region callback methods
    [SecurityCritical]
    private void OnReceive(IAsyncResult result)
    {
      if (_disposed)
        return;

      try
      {
        var bytesRead = _handler.EndReceive(result);
        if (bytesRead > 0)
        {
          OnPackagePartReceived();

          _received.Write(_buffer, 0, bytesRead);

          while (_packer.IsPackageReceived(_received))
          {
            var size = _packer.GetPackageSize(_received);
            if (size > MaxReceivedDataSize)
              throw new ModelException(ErrorCode.LargeReceivedData);

            var unpacked = _packer.Unpack<IPackage>(_received);

            var length = (int) _received.Length;

            _received.Position = 0;
            _received.SetLength(0);

            var restDataSize = length - size;
            if (restDataSize > 0)
            {
              var dataBuffer = _received.GetBuffer();
              _received.Write(dataBuffer, size, restDataSize);
            }

            switch (unpacked.Package.Id)
            {
              case RemoteInfoId:
                if (!_connectionInfoSent)
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

        _handler.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnReceive, null);
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
      if (_disposed)
        return;

      try
      {
        var size = _handler.EndSend(result);
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
      if (_disposed)
        return;

      try
      {
        _handler.EndDisconnect(result);
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
    /// Creates the object that contains info about connection.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual ConnectionInfo CreateConnectionInfo() { return new ConnectionInfo(); }

    /// <summary>
    /// Invokes when connection receive info about remote connection.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void OnInfoReceived(ConnectionInfo info) { }

    /// <summary>
    /// Invokes when connection receive package.
    /// </summary>
    /// <param name="args">Package event args.</param>
    [SecuritySafeCritical]
    protected abstract void OnPackageReceived(PackageReceivedEventArgs args);

    /// <summary>
    /// Invokes when data is sent or when error is thrown.
    /// </summary>
    /// <param name="args">Package event args.</param>
    [SecuritySafeCritical]
    protected abstract void OnPackageSent(PackageSendedEventArgs args);

    /// <summary>
    /// Invokes when part of data was read.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void OnPackagePartReceived() { }

    /// <summary>
    /// Invokes when disconnected.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void OnDisconnected(Exception e) { }

    // TODO: rus
    /// <summary>
    /// Происходит при SocketException. Без переопределение возращает всегда false.
    /// </summary>
    /// <param name="se">Словленое исключение.</param>
    /// <returns>Вовзращает значение говорящее о том, нужно ли дальше выкидывать исключение или оно обработано. true - обработано. false - не обработано.</returns>
    [SecuritySafeCritical]
    protected virtual bool OnSocketException(SocketException se) { return false; }
    #endregion

    #region private methods
    [SecurityCritical]
    private void SetRemoteInfo(IPackage package)
    {
      var remoteInfoPack = package as IPackage<ConnectionInfo>;
      if (remoteInfoPack == null)
        throw new ArgumentException("package isn't IPackage<ConnectionInfo>");

      // Set key
      _remoteInfo = remoteInfoPack.Content;
      var publicKey = CngKey.Import(_remoteInfo.PublicKey, CngKeyBlobFormat.EccPublicBlob);
      _packer.SetKey(_diffieHellman.DeriveKeyMaterial(publicKey));

      OnInfoReceived(_remoteInfo);
    }
    #endregion

    #region IDisposable
    [SecurityCritical]
    protected void ThrowIfDisposed()
    {
      if (_disposed)
        throw new ObjectDisposedException("Object disposed");
    }

    /// <summary>
    /// Clean the connection. After call connection can be reused.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void Clean()
    {
      if (_handler != null)
      {
        if (_handler.Connected)
        {
          _handler.Shutdown(SocketShutdown.Both);
          _handler.Disconnect(false);
          OnDisconnected(null);
        }

        _handler.Dispose();
        _handler = null;
      }

      if (_diffieHellman != null)
      {
        _diffieHellman.Dispose();
        _diffieHellman = null;
      }

      if (_received != null)
      {
        _received.Dispose();
        _received = null;
      }

      _connectionInfoSent = false;
      _remoteInfo = null;
    }

    /// <summary>
    /// Cealn the connection. After call connection cannot be reused.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void DisposeManagedResources()
    {
      Clean();
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;
      DisposeManagedResources();
    }
    #endregion

    #region utils
    // TODO: rus
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
