using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.Api.Client.Messages
{
  [Serializable]
  public class ClientSendPrivateMessageAction : IAction
  {
    private readonly string _reciverNick;
    private readonly string _text;

    /// <summary>
    /// Send private message to user.
    /// </summary>
    /// <param name="reciverNick">Reciver nick.</param>
    /// <param name="text">Message text.</param>
    [SecuritySafeCritical]
    public ClientSendPrivateMessageAction(string reciverNick, string text)
    {
      if (string.IsNullOrEmpty(reciverNick))
        throw new ArgumentException("reciverNick");

      if (string.IsNullOrEmpty(text))
        throw new ArgumentNullException("text");

      _reciverNick = reciverNick;
      _text = text;
    }

    [SecuritySafeCritical]
    public void Pefrorm()
    {
      var sendingContent = new ClientOutPrivateMessageCommand.MessageContent { Text = _text };
      ClientModel.Peer.SendMessage(_reciverNick, ClientOutPrivateMessageCommand.CommandId, sendingContent);
    }
  }
}
