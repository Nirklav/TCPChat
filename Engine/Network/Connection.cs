using System.Linq;
using Engine.Exceptions;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security;
using Engine.Helpers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Engine.Model.Common;
using Engine.Model.Common.Entities;

namespace Engine.Network
{
  /// <summary>
  /// Base connection.
  /// </summary>
  public abstract class Connection :
    MarshalByRefObject,
    IDisposable
  {
    #region nested types
    protected enum ConnectionState
    {
      Disconnected,
      ServerInfoWait,
      HandshakeRequestWait,
      HandshakeResponseWait,
      HandshakeAcceptWait,
      Connected
    }
    #endregion

    #region consts
    protected const long ServerInfo = 1;
    protected const long HandshakeRequest = 2;
    protected const long HandshakeResponse = 3;
    protected const long HandshakeAccepted = 4;
    
    private const int BufferSize = 4096;
    private const int MaxReceivedDataSize = 1024 * 1024;
    public const string TcpChatScheme = "tcpchat";
    #endregion

    #region fields
    [SecurityCritical] private readonly CertificatesStorage _trustedCertificates;
    [SecurityCritical] private readonly X509Certificate2 _localCertificate;
    [SecurityCritical] private X509Certificate2 _remoteCertificate;

    [SecurityCritical] private UserId _id;
    [SecurityCritical] private Socket _handler;
    [SecurityCritical] private ConnectionState _state;
    [SecurityCritical] private byte[] _generatedKey;
    [SecurityCritical] private CertificateStatus _remoteCertificateStatus;

    [SecurityCritical] private MemoryStream _received;
    [SecurityCritical] private byte[] _buffer;
    [SecurityCritical] private Packer _packer;

    [SecurityCritical] private readonly Logger _logger;

    [SecurityCritical] private volatile bool _disposed;
    #endregion

    #region constructors
    [SecurityCritical]
    protected Connection(CertificatesStorage trustedCertificates, X509Certificate2 certificate, Logger logger)
    {
      _trustedCertificates = trustedCertificates;
      _localCertificate = certificate;
      _state = ConnectionState.Disconnected;
      _logger = logger;
    }

    [SecurityCritical]
    protected void Construct(Socket handler, ConnectionState state)
    {
      if (handler == null)
        throw new ArgumentNullException(nameof(handler));

      if (!handler.Connected)
        throw new ArgumentException("Socket should be connected.");

      _handler = handler;
      _state = state;

      _received = new MemoryStream();
      _buffer = new byte[BufferSize];
      _packer = new Packer();

      _handler.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnReceive, null);
    }
    #endregion

    #region properties
    /// <summary>
    /// Connection id.
    /// </summary>
    public UserId Id
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
    /// Returns local certiticate.
    /// </summary>
    public X509Certificate2 LocalCertificate
    {
      [SecurityCritical]
      get { return _localCertificate; }
    }

    /// <summary>
    /// Returns remote certiticare.
    /// </summary>
    public X509Certificate2 RemoteCertiticate
    {
      [SecurityCritical]
      get { return _remoteCertificate; }
    }

    /// <summary>
    /// Returns remote certificate validation status. 
    /// </summary>
    public CertificateStatus RemoteCertificateStatus
    {
      [SecurityCritical]
      get
      {
        ThrowIfDisposed();
        return _remoteCertificateStatus;
      }
    }

    /// <summary>
    /// Returns true if client connected to server, otherwise false.
    /// </summary>
    public bool IsConnected
    {
      [SecuritySafeCritical]
      get { return _handler != null && _handler.Connected; }
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

    /// <summary>
    /// Returns true if Dispose method was invoked.
    /// </summary>
    protected bool IsClosed
    {
      [SecuritySafeCritical]
      get { return _disposed; }
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
    /// Starts disconnect from remote.
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
              case ServerInfo:
                var info = (IPackage<ServerInfo>)unpacked.Package;
                OnServerInfo(info.Content);
                unpacked.Dispose();
                break;
              case HandshakeRequest:
                var request = (IPackage<HandshakeRequest>)unpacked.Package;
                OnHandshakeRequest(request.Content);
                unpacked.Dispose();
                break;
              case HandshakeResponse:
                var response = (IPackage<HandshakeResponse>)unpacked.Package;
                OnHandshakeResponse(response.Content);
                unpacked.Dispose();
                break;
              case HandshakeAccepted:
                OnHandshakeAccepted();
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
    /// Invokes when connection receive version info.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void OnServerInfo(ServerInfo info)
    {
      try
      {
        if (_state != ConnectionState.ServerInfoWait)
          throw new InvalidOperationException("Connection must be in ServerInfoWait state");

        _state = ConnectionState.HandshakeResponseWait;

        var request = new HandshakeRequest();
        request.RawX509Certificate = _localCertificate.Export(X509ContentType.Cert);
        SendMessage(HandshakeRequest, request);
      }
      catch (Exception e)
      {
        OnHandshakeException(e);
      }
    }

    /// <summary>
    /// Invokes when connection receive request handshake from remote connection.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void OnHandshakeRequest(HandshakeRequest request)
    {
      try
      {
        if (_state != ConnectionState.HandshakeRequestWait)
          throw new InvalidOperationException("Connection must be in HandshakeRequestWait state");

        var remoteCertificate = new X509Certificate2(request.RawX509Certificate);
        if (remoteCertificate.HasPrivateKey)
          throw new InvalidOperationException("Remote certificate has private key");

        if (!ValidateCertificate(remoteCertificate))
          throw new InvalidOperationException("Remote certiticate not validated");

        _remoteCertificate = remoteCertificate;

        byte[] key;
        using (var rng = new RNGCryptoServiceProvider())
        {
          _generatedKey = new byte[32];
          rng.GetBytes(_generatedKey);

          var alg = _remoteCertificate.PublicKey.Key;
          if (alg is RSACryptoServiceProvider rsa)
            key = rsa.Encrypt(_generatedKey, false);
          else
            throw new InvalidOperationException("not supported key algorithm");
        }

        SendMessage(HandshakeResponse, new HandshakeResponse
        {
          AlgorithmId = AlgorithmId.Aes256CBC,
          EncryptedKey = key,
          RawX509Certificate = _localCertificate.Export(X509ContentType.Cert)
        });

        _state = ConnectionState.HandshakeAcceptWait;
      }
      catch (Exception e)
      {
        _remoteCertificate = null;

        OnHandshakeException(e);
      }
    }

    /// <summary>
    /// Invokes when connection receive response handshake from remote connection.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void OnHandshakeResponse(HandshakeResponse response)
    {
      try
      {
        if (_state != ConnectionState.HandshakeResponseWait)
          throw new InvalidOperationException("Connection must be in HandshakeResponseWait state");

        var remoteCertificate = new X509Certificate2(response.RawX509Certificate);
        if (remoteCertificate.HasPrivateKey)
          throw new InvalidOperationException("Remote certificate has private key");

        if (!ValidateCertificate(remoteCertificate))
          throw new InvalidOperationException("Remote certiticate not validated");

        _remoteCertificate = remoteCertificate;

        byte[] clearKey;
        var alg = _localCertificate.PrivateKey;
        if (alg is RSACryptoServiceProvider rsa)
          clearKey = rsa.Decrypt(response.EncryptedKey, false);
        else
          throw new InvalidOperationException("not supported key algorithm");

        SendMessage(HandshakeAccepted);

        _packer.SetKey(clearKey);
        _state = ConnectionState.Connected;
      }
      catch (Exception e)
      {
        _remoteCertificate = null;

        OnHandshakeException(e);
      }
    }

    /// <summary>
    /// Invokes when remote connection accepted handshake.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void OnHandshakeAccepted()
    {
      try
      {
        if (_state != ConnectionState.HandshakeAcceptWait)
          throw new InvalidOperationException("Connection must be in HandshakeAcceptWait state");

        _packer.SetKey(_generatedKey);
        _generatedKey = null;
        _state = ConnectionState.Connected;
      }
      catch (Exception e)
      {
        OnHandshakeException(e);
      }
    }

    /// <summary>
    /// Validates certificate.
    /// </summary>
    /// <param name="remote">Remote certiticate that should be validated.</param>
    /// <returns>Method returns true if certificate is valid otherwise false.</returns>
    [SecuritySafeCritical]
    protected virtual bool ValidateCertificate(X509Certificate2 remote)
    {
      _remoteCertificateStatus = GetCertificateValidationStatus(remote, _trustedCertificates);
      return _remoteCertificateStatus == CertificateStatus.SelfSigned
        || _remoteCertificateStatus == CertificateStatus.Trusted;
    }

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

    /// <summary>
    /// Invokes when in handshake protocol an exception was thrown.
    /// </summary>
    [SecuritySafeCritical]
    protected virtual void OnHandshakeException(Exception e)
    {
      _logger.Write(e);
    }

    /// <summary>
    /// Provides an possibility to process SocketException. If exception was processed method should returns true. By default method returns false.
    /// </summary>
    /// <param name="se">Catched exceptions.</param>
    /// <returns>If exception was processed method should returns true otherwise false.</returns>
    [SecuritySafeCritical]
    protected virtual bool OnSocketException(SocketException se) { return false; }
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
          try
          {
            _handler.Shutdown(SocketShutdown.Both);
            _handler.Disconnect(false);
          }
          catch (Exception e)
          {
            _logger.Write(e);
          }
          finally
          {
            OnDisconnected(null);
          }
        }

        _handler.Dispose();
        _handler = null;
      }

      if (_received != null)
      {
        _received.Dispose();
        _received = null;
      }
    }

    /// <summary>
    /// Clean the connection. After call connection cannot be reused.
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
    public static CertificateStatus GetCertificateValidationStatus(X509Certificate2 certificate, CertificatesStorage trustedCertificates)
    {
      var chain = new X509Chain();
      chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

      if (!chain.Build(certificate))
        return CertificateStatus.Untrusted;
      else
      {
        if (chain.ChainStatus.Any(s => s.Status == X509ChainStatusFlags.UntrustedRoot))
        {
          if (trustedCertificates != null && trustedCertificates.Exist(certificate))
            return CertificateStatus.Trusted;
          else if (certificate.Issuer == certificate.Subject)
            return CertificateStatus.SelfSigned;
          else
            return CertificateStatus.Untrusted;
        }
        return CertificateStatus.Trusted;
      }
    }

    /// <summary>
    /// Creates tcp chat uri.
    /// </summary>
    /// <param name="address">Server address.</param>
    /// <param name="port">Server port.</param>
    /// <returns>Uri of TcpChat server.</returns>
    public static Uri CreateTcpchatUri(IPAddress address, int port)
    {
      if (address.AddressFamily == AddressFamily.InterNetwork)
        return new Uri(string.Format("{0}://{1}:{2}/", TcpChatScheme, address, port));
      if (address.AddressFamily == AddressFamily.InterNetworkV6)
        return new Uri(string.Format("{0}://[{1}]:{2}/", TcpChatScheme, address, port));
      throw new ArgumentException("Not supported address");
    }

    /// <summary>
    /// Creates tcp chat uri.
    /// </summary>
    /// <param name="hostOrAddress">Host or address.</param>
    /// <returns>Uri of TcpChat server.</returns>
    public static Uri CreateTcpchatUri(string hostOrAddress)
    {
      string hostStr;
      int port;

      if (hostOrAddress.Contains(':'))
      {
        var parts = hostOrAddress.Split(':');
        hostStr = parts[0];
        var portStr = parts[1];

        if (!int.TryParse(portStr, out port))
          throw new ArgumentException("host");
      }
      else
      {
        hostStr = hostOrAddress;
        port = 10021;
      }
      
      if (IPAddress.TryParse(hostStr, out var address))
        return CreateTcpchatUri(address, port);
      else
        return new Uri(string.Format("{0}://{1}:{2}/", TcpChatScheme, hostStr, port));

      throw new ArgumentException("Not supported address");
    }

    /// <summary>
    /// Checks port for use.
    /// </summary>
    /// <param name="port">Port to check.</param>
    /// <returns>Returns true if port is free.</returns>
    [SecuritySafeCritical]
    public static bool TcpPortIsAvailable(int port)
    {
      if (port < 0 || port > ushort.MaxValue)
        return false;

      var properties = IPGlobalProperties.GetIPGlobalProperties();
      var connections = properties.GetActiveTcpConnections();
      var listeners = properties.GetActiveTcpListeners();

      return connections.All(c => c.LocalEndPoint.Port != port) && listeners.All(l => l.Port != port);
    }

    /// <summary>
    /// Returns ip of current machine.
    /// </summary>
    /// <param name="type">Address type.</param>
    /// <returns>IPAddress of current machine.</returns>
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
