using Engine.Api.Client.Messages;
using Engine.Model.Server;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Api.Server.Messages
{
  [SecuritySafeCritical]
  public class ServerRemoveMessagesAction : IAction
  {
    private readonly string _nick;
    private readonly string _roomName;
    private readonly long[] _ids;

    [SecuritySafeCritical]
    public ServerRemoveMessagesAction(string nick, string roomName, IEnumerable<long> ids)
    {
      _nick = nick;
      _roomName = roomName;
      _ids = ids as long[] ?? ids.ToArray();
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var messageContent = new ClientRemoveMessagesCommand.MessageContent { RoomName = _roomName, Ids = _ids };
      ServerModel.Server.SendMessage(_nick, ClientRemoveMessagesCommand.CommandId, messageContent);
    }
  }
}
