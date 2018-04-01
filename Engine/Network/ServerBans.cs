using Engine.Model.Common.Entities;
using System.Collections.Generic;
using System.Net;
using System.Security;

namespace Engine.Network
{
  public class ServerBans
  {
    private readonly AsyncServer _server;

    private readonly object _syncObject = new object();
    private readonly Dictionary<UserId, IPAddress> _connectionIdToAddress = new Dictionary<UserId, IPAddress>();
    private readonly Dictionary<IPAddress, UserId> _addressToConnectionId = new Dictionary<IPAddress, UserId>();

    [SecuritySafeCritical]
    public ServerBans(AsyncServer server)
    {
      _server = server;
    }

    [SecuritySafeCritical]
    public void Ban(UserId connectionId)
    {
      var ipAddress = _server.GetIp(connectionId);
      lock (_syncObject)
      {
        _connectionIdToAddress.Add(connectionId, ipAddress);
        _addressToConnectionId.Add(ipAddress, connectionId);
      }
    }

    [SecuritySafeCritical]
    public void Unban(UserId connectionId)
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
    public bool IsBanned(UserId connectionId)
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
    public UserId Who(IPAddress address)
    {
      lock (_syncObject)
      {
        _addressToConnectionId.TryGetValue(address, out UserId connectionId);
        return connectionId;
      }
    }

    [SecuritySafeCritical]
    public List<UserId> FindAll(string nick)
    {
      lock (_syncObject)
      {
        var result = new List<UserId>();
        foreach (var userId in _connectionIdToAddress.Keys)
        {
          if (string.Equals(userId.Nick, nick, System.StringComparison.Ordinal))
            result.Add(userId);
        }
        return result;
      }
    }
  }
}
