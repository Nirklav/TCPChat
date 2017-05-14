using Engine.Api.Client.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using System;
using System.Security;

namespace Engine.Api.Server.Messages
{
  [Serializable]
  public class ServerSendSystemMessageAction : IAction
  {
    private readonly string _nick;
    private readonly SystemMessageId _message;
    private readonly string[] _formatParams;

    /// <summary>
    /// Send system message to user.
    /// </summary>
    /// <param name="nick">Reciver of system message.</param>
    /// <param name="message">System message id.</param>
    /// <param name="formatParams">Format params for message.</param>
    [SecuritySafeCritical]
    public ServerSendSystemMessageAction(string nick, SystemMessageId message, params string[] formatParams)
    {
      _nick = nick;
      _message = message;
      _formatParams = formatParams;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var sendingContent = new ClientOutSystemMessageCommand.MessageContent { Message = _message, FormatParams = _formatParams };
      ServerModel.Server.SendMessage(_nick, ClientOutSystemMessageCommand.CommandId, sendingContent);
    }
  }
}
