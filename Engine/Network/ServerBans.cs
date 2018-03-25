using System.Collections.Generic;
using System.Net;
using System.Security;

namespace Engine.Network
{
  public class ServerBans
  {
    private readonly AsyncServer _server;

    private readonly object _syncObject = new object();
    private readonly Dictionary<string, IPAddress> _connectionIdToAddress = new Dictionary<string, IPAddress>();
    private readonly Dictionary<IPAddress, string> _addressToConnectionId = new Dictionary<IPAddress, string>();

    [SecuritySafeCritical]
    public ServerBans(AsyncServer server)
    {
      _server = server;
    }

    [SecuritySafeCritical]
    public void Ban(string connectionId)
    {
      var ipAddress = _server.GetIp(connectionId);
      lock (_syncObject)
      {
        _connectionIdToAddress.Add(connectionId, ipAddress);
        _addressToConnectionId.Add(ipAddress, connectionId);
      }
    }

    [SecuritySafeCritical]
    public void Unban(string connectionId)
    {
      lock (_syncObject)
      {
        if (_connectionIdToAddress.TryGetValue(connectionId, out IPAddress address))
        {
          _connectionIdToAddress.Remove(connectionId);
          _addressToConnectionId.Remove(address);
        }
      }
    }

    [SecuritySafeCritical]
    public bool IsBanned(string connectionId)
    {
      lock (_syncObject)
        return _connectionIdToAddress.ContainsKey(connectionId);
    }

    [SecuritySafeCritical]
    public bool IsBanned(IPAddress address)
    {
      lock (_syncObject)
        return _addressToConnectionId.ContainsKey(address);
    }

    [SecuritySafeCritical]
    public string Who(IPAddress address)
    {
      lock (_syncObject)
      {
        _addressToConnectionId.TryGetValue(address, out string connectionId);
        return connectionId;
      }
    }
  }
}
