using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Api.Client.Messages
{
  [Serializable]
  public class ClientSendPrivateMessageAction : IAction
  {
    private readonly UserId _reciverId;
    private readonly string _text;

    /// <summary>
    /// Send private message to user.
    /// </summary>
    /// <param name="reciverId">Reciver id.</param>
    /// <param name="text">Message text.</param>
    [SecuritySafeCritical]
    public ClientSendPrivateMessageAction(UserId reciverId, string text)
    {
      if (reciverId == UserId.Empty)
        throw new ArgumentException(nameof(reciverId));

      if (string.IsNullOrEmpty(text))
        throw new ArgumentNullException(nameof(text));

      _reciverId = reciverId;
      _text = text;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var sendingContent = new ClientOutPrivateMessageCommand.MessageContent { Text = _text };
      ClientModel.Peer.SendMessage(_reciverId, ClientOutPrivateMessageCommand.CommandId, sendingContent);
    }
  }
}
