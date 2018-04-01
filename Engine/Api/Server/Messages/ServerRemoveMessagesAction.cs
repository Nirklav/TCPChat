using Engine.Api.Client.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Api.Server.Messages
{
  [SecuritySafeCritical]
  public class ServerRemoveMessagesAction : IAction
  {
    private readonly UserId _userId;
    private readonly string _roomName;
    private readonly long[] _ids;

    [SecuritySafeCritical]
    public ServerRemoveMessagesAction(UserId userId, string roomName, IEnumerable<long> ids)
    {
      _userId = userId;
      _roomName = roomName;
      _ids = ids as long[] ?? ids.ToArray();
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var messageContent = new ClientRemoveMessagesCommand.MessageContent { RoomName = _roomName, Ids = _ids };
      ServerModel.Server.SendMessage(_userId, ClientRemoveMessagesCommand.CommandId, messageContent);
    }
  }
}
